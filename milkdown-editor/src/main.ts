import { Crepe } from "@milkdown/crepe";
import "@milkdown/crepe/theme/common/style.css";
import "@milkdown/crepe/theme/frame-dark.css";
import { gfm } from "@milkdown/preset-gfm";
import { replaceAll } from "@milkdown/kit/utils";

declare global {
  interface Window {
    editorBridge: any;
    chrome: any;
  }
}

const crepe = new Crepe({
  root: "#app",
  defaultValue: "",
});

crepe.editor.use(gfm);

crepe.on((listener) => {
  listener.markdownUpdated((_ctx, markdown, _prevMarkdown) => {
    if (window.editorBridge && window.editorBridge.isUpdating) {
      return;
    }

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage({
        type: "contentChanged",
        content: markdown,
      });
    }
  });
});

await crepe.create();

window.editorBridge = {
  isReady: true,
  isUpdating: false,
  setContent: (markdown: string) => {
    window.editorBridge.isUpdating = true;
    crepe.editor.action(replaceAll(markdown));
    // Allow the event loop to clear before unsetting the flag
    setTimeout(() => {
      window.editorBridge.isUpdating = false;
    }, 50);
  },
};
