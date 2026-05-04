import { Editor } from "@tiptap/core";
import StarterKit from "@tiptap/starter-kit";
import { Markdown } from "@tiptap/markdown";
import Image from "@tiptap/extension-image";
import { ListKit } from "@tiptap/extension-list";
import { TableKit } from "@tiptap/extension-table";
import Link from "@tiptap/extension-link";
import CodeBlockLowlight from "@tiptap/extension-code-block-lowlight";
import TextAlign from "@tiptap/extension-text-align";
import { common, createLowlight } from "lowlight";
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
    // TaskList + TaskItem: GFM task list syntax (- [ ] / - [x])
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
    // Replaces StarterKit's plain CodeBlock. Language is auto-detected from the fence info.
    // Ref: https://tiptap.dev/docs/editor/extensions/nodes/code-block-lowlight
    CodeBlockLowlight.configure({
      lowlight,
      defaultLanguage: "plaintext",
    }),
    // TextAlign: adds text-align support to headings and paragraphs
    // Enables <p align="center"> and toolbar alignment commands
    // Ref: https://tiptap.dev/docs/editor/extensions/functionality/text-align
    TextAlign.configure({
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

window.editorBridge = {
  isReady: true,
  isUpdating: false,

  // Load content from C# (accepts both Markdown and inline HTML)
  setContent: (content: string) => {
    window.editorBridge.isUpdating = true;
    editor.commands.setContent(content, {
      emitUpdate: false,
      contentType: "markdown",
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
  // C# calls these via ExecuteScriptAsync, completely decoupled from Tiptap internals
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
