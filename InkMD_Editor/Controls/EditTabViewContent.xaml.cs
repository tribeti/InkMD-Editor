using InkMD_Editor.Interfaces;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using TextControlBoxNS;

namespace InkMD_Editor.Controls;

public sealed partial class EditTabViewContent : UserControl, IEditableContent
{
    public EditTabViewModel ViewModel { get; set; } = new();
    public EditTabViewContent ()
    {
        InitializeComponent();
        EditBox.EnableSyntaxHighlighting = true;
        EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
    }

    public void SetContent (string text , string? fileName)
    {
        EditBox.SetText(text);
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
    }

    public string GetContent () => EditBox.GetText() ?? string.Empty;

    public string GetFilePath () => ViewModel.FilePath ?? string.Empty;

    public string GetFileName () => ViewModel.FileName ?? string.Empty;

    public void SetFilePath (string filePath , string fileName) => ViewModel.SetFilePath(filePath , fileName);

    public void Undo () => EditBox?.Undo();

    public void Redo () => EditBox?.Redo();

    public void Cut () => EditBox?.Cut();

    public void Copy () => EditBox?.Copy();

    public void Paste () => EditBox?.Paste();
}
