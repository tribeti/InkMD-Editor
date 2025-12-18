using InkMD_Editor.Interfaces;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Controls;

public sealed partial class EditTabViewContent : UserControl, IEditableContent
{
    public EditTabViewModel ViewModel { get; set; } = new();
    public EditTabViewContent ()
    {
        InitializeComponent();
    }

    public void SetContent (string text , string? fileName)
    {
        EditBox.Document.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
    }

    public string GetContent ()
    {
        EditBox.Document.GetText(TextGetOptions.None , out string currentText);
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
