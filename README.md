<p align="center">
  <img width="180" src="https://github.com/tribeti/InkMD-Assets/blob/main/Square400x400Logo.png?raw=true" alt="InkMD Logo" />
</p>

<h1 align="center">InkMD Editor</h1>

<p align="center">
  <i>A modern Markdown editor for Windows with real-time preview, powerful file management, and a distraction-free writing experience.</i>
</p>

<p align="center">
  <img src="https://img.shields.io/github/stars/tribeti/InkMD-Editor?style=for-the-badge&logo=github&color=f1c40f" alt="Stars" />
  <img src="https://img.shields.io/github/forks/tribeti/InkMD-Editor?style=for-the-badge&logo=github&color=3498db" alt="Forks" />
  <img src="https://img.shields.io/github/license/tribeti/InkMD-Editor?style=for-the-badge&logo=opensourceinitiative&color=2ecc71" alt="License" />
</p>

---

## About InkMD

**InkMD Editor** is a lightweight yet feature-rich Markdown editor built for developers, technical writers, and note-takers who need speed and simplicity without sacrificing functionality.

Designed with performance in mind, InkMD combines a smooth writing workflow with live preview rendering, smart file management, and a clean modern interface — making it ideal for documentation, note-taking, blogging, and everyday Markdown editing.

---

## Features

- **Live Preview Rendering**  
  Instantly preview Markdown as you type with responsive, real-time rendering.

- **Rich Editing Experience**  
  Syntax highlighting, smooth editing interactions, and a distraction-free writing layout.

- **Integrated File Explorer**  
  Manage folders, recent files, and auto-save workflows directly inside the app.

- **Built-in Templates**  
  Quickly create README files, journals, notes, and documentation templates.

- **Customizable Appearance**  
  Switch between Light/Dark themes and personalize editor fonts.

- **Keyboard Shortcut Support**  
  Improve productivity with intuitive shortcuts for editing and navigation.

---

## Screenshots

<p align="center">
  <img src="https://github.com/tribeti/InkMD-Assets/blob/main/appscreenshot.png?raw=true" alt="Main Editor View" />
</p>

---

## Installation

### Download Prebuilt Version

Download the latest release from:

- [GitHub Releases](https://github.com/tribeti/InkMD-Editor/releases)
- [Microsoft Store](https://apps.microsoft.com/detail/9NNX83392BPP)

---

## Build From Source

<details>
<summary><b>Build instructions</b></summary>

### Prerequisites

InkMD requires:

- **Visual Studio 2022** or later
- **Windows 10/11**
- **Node.js + npm**

Install these Visual Studio workloads:

- WinUI application development
- .NET desktop development
- Windows 10 SDK (10.0.19041.0 or later)

---

### Clone the repository

```bash
git clone --recurse-submodules https://github.com/tribeti/InkMD-Editor.git
````

---

### Build the preview editor

The Markdown preview pane uses **Milkdown**, so build it first:

```bash
cd milkdown-editor
npm install
npm run build
```

---

### Run the application

Open the solution in **Visual Studio 2022** and press:

```bash
F5
```

</details>

---

## Contributing

Contributions are welcome and appreciated.

1. Fork the repository
2. Create your branch:

```bash
git checkout -b feature/my-feature
```

3. Commit your changes:

```bash
git commit -m "Add my feature"
```

4. Push to GitHub:

```bash
git push origin feature/my-feature
```

5. Open a Pull Request

---

## 💎 Special Thanks

This project stands on the shoulders of giants. A huge shoutout to these amazing projects and libraries that made InkMD Editor possible:

- [**TextControlBox-WinUI**](https://github.com/FrozenAssassine/TextControlBox-WinUI) (by JuliusKirsch) for providing the powerful text editing foundation that powers InkMD's core writing experience.

- [**Markdig**](https://github.com/xoofx/markdig) for delivering a fast, reliable, and standards-compliant Markdown engine for .NET.

- [**Windows Community Toolkit**](https://github.com/CommunityToolkit/Windows) for offering essential utilities and extensions that simplify Windows application development.

- [**Milkdown**](https://milkdown.dev/) for enabling the rich and modern Markdown preview experience inside InkMD.

- [**Advanced Installer**](https://www.advancedinstaller.com/) for providing professional packaging tools that help deliver InkMD to users smoothly.

And most importantly, heartfelt thanks to every contributor, maintainer, and open-source creator whose work inspires and supports projects like this every day.

---

## License

Licensed under the **MIT License**.

See the [LICENSE](https://github.com/tribeti/InkMD-Editor/blob/master/LICENSE) file for details.

---

<p align="center">
  <b>Happy Writing!</b>
</p>
