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

    public string GetContent ()
    {
        string currentText = EditBox.GetText();
        if ( !string.IsNullOrEmpty(currentText) && currentText.EndsWith('\r') )
        {
            currentText = currentText [..^1];
        }
        return currentText;
    }

    public string GetFilePath () => ViewModel.FilePath ?? string.Empty;

    public string GetFileName () => ViewModel.FileName ?? string.Empty;

    public void SetFilePath (string filePath , string fileName) => ViewModel.SetFilePath(filePath , fileName);
}
