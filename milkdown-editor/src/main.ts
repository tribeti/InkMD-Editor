import { Crepe } from "@milkdown/crepe";
import "@milkdown/crepe/theme/common/style.css";
import "@milkdown/crepe/theme/frame-dark.css";
import { gfm } from "@milkdown/preset-gfm";

const markdown = `# Milkdown Editor Crepe
> This is a demo for using [Milkdown](https://milkdown.dev) editor crepe.
`;

const crepe = new Crepe({
  root: "#app",
  defaultValue: markdown,
});

// github flavor markdown
crepe.editor.use(gfm);

await crepe.create();
