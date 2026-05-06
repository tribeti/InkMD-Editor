import { Editor } from "@tiptap/core";
import StarterKit from "@tiptap/starter-kit";
import { Markdown } from "@tiptap/markdown";
import Image from "@tiptap/extension-image";
import { ListKit } from "@tiptap/extension-list";
import { TableKit } from "@tiptap/extension-table";
import Link from "@tiptap/extension-link";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import TextAlign from "@tiptap/extension-text-align";
import {
  Details,
  DetailsSummary,
  DetailsContent,
} from "@tiptap/extension-details";
import { common, createLowlight } from "lowlight";
import { marked } from "marked";
import "./style.css";

// Ref: https://github.com/wooorm/lowlight?tab=readme-ov-file#syntaxes
const lowlight = createLowlight(common);

declare global {
  interface Window {
    editorBridge: EditorBridge;
    chrome: any;
  }
}

interface EditorBridge {
  isReady: boolean;
  isUpdating: boolean;
  setContent: (content: string) => void;
  setTheme: (theme: string) => void;
  setFontFamily: (fontFamily: string) => void;
  format: {
    toggleBold: () => void;
    toggleItalic: () => void;
    toggleStrike: () => void;
    toggleCode: () => void;
    setHeading: (level: 1 | 2 | 3 | 4 | 5 | 6) => void;
    toggleBulletList: () => void;
    toggleOrderedList: () => void;
    toggleBlockquote: () => void;
    setTextAlign: (align: "left" | "center" | "right" | "justify") => void;
    undo: () => void;
    redo: () => void;
  };
}

const editor = new Editor({
  element: document.querySelector("#app") as HTMLElement,
  extensions: [
    StarterKit.configure({
      codeBlock: false,
    }),
    // Image: renders ![alt](url) AND raw <img src=""> HTML tags
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/image
    Image.configure({
      inline: true,
      allowBase64: true,
    }),
    // TaskList + TaskItem
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/task-list
    ListKit.configure({
      taskItem: { nested: true },
    }),
    TableKit.configure({
      table: { resizable: false },
    }),
    Link.configure({
      openOnClick: false,
      autolink: true,
      HTMLAttributes: {
        rel: "noopener noreferrer",
        target: "_blank",
      },
    }),
    // CodeBlockLowlight: syntax-highlighted code blocks via lowlight (highlight.js)
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/code-block-lowlight
    CodeBlockLowlight.configure({
      lowlight,
      defaultLanguage: "plaintext",
    }),
    // TextAlign: adds text-align support to headings and paragraphs
    // Ref: https://tiptap.dev/docs/editor/extensions/functionality/text-align
    TextAlign.extend({
      addGlobalAttributes() {
        return [
          {
            types: ["heading", "paragraph"],
            attributes: {
              textAlign: {
                default: this.options.defaultAlignment,
                parseHTML: (element) =>
                  element.getAttribute("align") ||
                  element.style.textAlign ||
                  this.options.defaultAlignment,
                renderHTML: (attributes) => {
                  if (
                    !attributes.textAlign ||
                    attributes.textAlign === this.options.defaultAlignment
                  ) {
                    return {};
                  }
                  return { style: `text-align: ${attributes.textAlign}` };
                },
              },
            },
          },
        ];
      },
    }).configure({
      types: ["heading", "paragraph"],
      defaultAlignment: "left",
    }),
    // Details / Summary / DetailsContent: renders <details> / <summary> HTML tags
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/details
    Details.configure({
      renderToggleButton({ element, isOpen }) {
        element.setAttribute(
          "aria-label",
          isOpen ? "Collapse details content" : "Expand details content",
        );
        element.innerHTML = `<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 10 10" width="10" height="10"><path d="M2 1 L8 5 L2 9" fill="none" stroke="currentColor" stroke-width="1.8" stroke-linecap="round" stroke-linejoin="round"/></svg>`;
      },
    }),
    DetailsSummary,
    DetailsContent,
    Markdown.configure({
      markedOptions: {
        gfm: true,
        breaks: false,
      },
    }),
  ],
  content: "",
  onUpdate: ({ editor }) => {
    // Guard against self-triggered updates from setContent()
    if (window.editorBridge && window.editorBridge.isUpdating) {
      return;
    }
    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        type: "contentChanged",
        content: editor.getMarkdown(),
      });
    }
  },
});

function preprocessMarkdownWithDetails(markdown: string): string {
  const segments: string[] = [];
  let lastIndex = 0;
  let i = 0;

  while (i < markdown.length) {
    const openMatch = markdown.indexOf("<details", i);
    if (openMatch === -1) {
      const rest = markdown.slice(lastIndex);
      if (rest) segments.push(String(marked.parse(rest)));
      break;
    }

    const before = markdown.slice(lastIndex, openMatch);
    if (before) segments.push(String(marked.parse(before)));

    const openTagEnd = markdown.indexOf(">", openMatch);
    if (openTagEnd === -1) {
      i = openMatch + 1;
      lastIndex = openMatch;
      continue;
    }

    let depth = 1;
    let searchFrom = openTagEnd + 1;
    let closeMatch = -1;

    while (depth > 0 && searchFrom < markdown.length) {
      const nextOpen = markdown.indexOf("<details", searchFrom);
      const nextClose = markdown.indexOf("</details>", searchFrom);

      if (nextClose === -1) break;

      if (nextOpen !== -1 && nextOpen < nextClose) {
        depth++;
        searchFrom = nextOpen + 8;
      } else {
        depth--;
        if (depth === 0) {
          closeMatch = nextClose;
        }
        searchFrom = nextClose + 10;
      }
    }

    if (closeMatch === -1) {
      const rest = markdown.slice(openMatch);
      segments.push(String(marked.parse(rest)));
      lastIndex = markdown.length;
      break;
    }

    const innerContent = markdown.slice(openTagEnd + 1, closeMatch);
    const summaryMatch = innerContent.match(
      /^[\s\S]*?<summary>([\s\S]*?)<\/summary>/,
    );
    let summaryHtml = "";
    let bodyMarkdown = innerContent;

    if (summaryMatch) {
      summaryHtml = summaryMatch[1].trim();
      bodyMarkdown = innerContent.slice(summaryMatch[0].length);
    }

    const bodyHtml = String(marked.parse(bodyMarkdown.trim()));

    segments.push(
      `<details><summary>${summaryHtml}</summary>${bodyHtml}</details>`,
    );

    lastIndex = closeMatch + 10;
    i = lastIndex;
  }

  if (segments.length === 0) {
    return String(marked.parse(markdown));
  }

  return segments.join("\n");
}

window.editorBridge = {
  isReady: true,
  isUpdating: false,

  // Load content from C#
  setContent: (content: string) => {
    window.editorBridge.isUpdating = true;
    const processedHtml = preprocessMarkdownWithDetails(content);
    editor.commands.setContent(processedHtml, {
      emitUpdate: false,
      contentType: "html",
    });
    setTimeout(() => {
      window.editorBridge.isUpdating = false;
    }, 100);
  },

  // Switch dark/light theme from WinUI
  setTheme: (theme: string) => {
    document.documentElement.classList.remove("dark", "light");
    document.documentElement.classList.add(theme);
  },

  // Change editor font — call from WinUI Settings page
  // Example (C#): ExecuteScriptAsync("window.editorBridge.setFontFamily('Consolas')")
  setFontFamily: (fontFamily: string) => {
    document.documentElement.style.setProperty(
      "--editor-font-family",
      fontFamily,
    );
  },

  // Formatting commands — Abstraction Layer for WinUI Toolbar
  // Ref: https://tiptap.dev/docs/editor/api/commands
  format: {
    toggleBold: () => editor.chain().focus().toggleBold().run(),
    toggleItalic: () => editor.chain().focus().toggleItalic().run(),
    toggleStrike: () => editor.chain().focus().toggleStrike().run(),
    toggleCode: () => editor.chain().focus().toggleCode().run(),
    setHeading: (level: 1 | 2 | 3 | 4 | 5 | 6) =>
      editor.chain().focus().toggleHeading({ level }).run(),
    toggleBulletList: () => editor.chain().focus().toggleBulletList().run(),
    toggleOrderedList: () => editor.chain().focus().toggleOrderedList().run(),
    toggleBlockquote: () => editor.chain().focus().toggleBlockquote().run(),
    setTextAlign: (align: "left" | "center" | "right" | "justify") =>
      editor.chain().focus().setTextAlign(align).run(),
    undo: () => editor.chain().focus().undo().run(),
    redo: () => editor.chain().focus().redo().run(),
  },
};

// Notify WinUI host that the editor bridge is fully ready (event-driven, no polling)
if (window.chrome?.webview) {
  window.chrome.webview.postMessage({ type: "ready" });
}
