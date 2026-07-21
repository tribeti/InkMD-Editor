import { Editor } from "@tiptap/core";
import StarterKit from "@tiptap/starter-kit";
import { Markdown } from "@tiptap/markdown";
import Image from "@tiptap/extension-image";
import TaskList from "@tiptap/extension-task-list";
import TaskItem from "@tiptap/extension-task-item";
import { TableKit } from "@tiptap/extension-table";
import Link from "@tiptap/extension-link";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import TextAlign from "@tiptap/extension-text-align";
import { common, createLowlight } from "lowlight";

import "./style.css";

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
  insertContent: (markdown: string) => void;
  cut: () => void;
  copy: () => void;
  paste: () => void;
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

document.addEventListener(
  "keydown",
  (e) => {
    if (e.ctrlKey && (e.key === "s" || e.key === "S")) {
      e.preventDefault();
      window.chrome?.webview?.postMessage({
        type: "saveRequest",
        saveAs: e.shiftKey,
      });
    }
  },
  true,
);

let editor: Editor;

editor = new Editor({
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
      HTMLAttributes: {
        referrerpolicy: "no-referrer",
      },
    }),
    // TaskList + TaskItem: GFM checkbox lists
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/task-list
    TaskList,
    TaskItem.configure({ nested: true }),
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
    Markdown.configure({
      markedOptions: {
        gfm: true,
        breaks: false,
      },
    }),
  ],
  content: "",
  editorProps: {
    handlePaste(view, event) {
      const clipboardData = event.clipboardData;
      if (!clipboardData) return false;

      // If the clipboard contains HTML we let Tiptap handle it natively
      const html = clipboardData.getData("text/html");
      if (html && html.trim().length > 0) return false;

      const text = clipboardData.getData("text/plain");
      if (!text || text.trim().length === 0) return false;

      // Detect markdown heuristically
      const markdownPattern =
        /^#{1,6}\s|\*\*|__|\[.+?\]\(.+?\)|^[-*+]\s|^\d+\.\s|^>\s|`|!\[/m;
      if (!markdownPattern.test(text)) return false;
      event.preventDefault();
      editor.commands.insertContent(text, {
        contentType: "markdown",
      });
      return true;
    },
  },
  onUpdate: ({ editor }) => {
    // Guard against self-triggered updates from setContent()k
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

window.editorBridge = {
  isReady: true,
  isUpdating: false,

  // Load content from C#
  setContent: (content: string) => {
    window.editorBridge.isUpdating = true;
    try {
      editor.commands.setContent(content, {
        emitUpdate: false,
        contentType: "markdown",
      });
    } finally {
      window.editorBridge.isUpdating = false;
    }
  },

  // Switch dark/light theme from WinUI
  setTheme: (theme: string) => {
    document.documentElement.classList.remove("dark", "light");
    document.documentElement.classList.add(theme);
  },

  // Change editor font — call from WinUI Settings page
  setFontFamily: (fontFamily: string) => {
    document.documentElement.style.setProperty(
      "--editor-font-family",
      fontFamily,
    );
  },

  // Insert markdown content at the current cursor position
  insertContent: (markdown: string) => {
    editor.commands.insertContent(markdown, { contentType: "markdown" });
  },

  // Clipboard commands forwarded from WinUI host
  cut: () => {
    document.execCommand("cut");
  },
  copy: () => {
    document.execCommand("copy");
  },
  paste: () => {
    document.execCommand("paste");
  },

  // Formatting commands
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

const win = window as any;
if (win.__initialContent) {
  editor.commands.setContent(win.__initialContent, {
    emitUpdate: false,
    contentType: "markdown",
  });
  win.__initialContent = null;
}

// Notify WinUI host that the editor bridge is fully ready (event-driven, no polling)
if (window.chrome?.webview) {
  window.chrome.webview.postMessage({ type: "ready" });
}
