using InkMD_Editor.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Controls;

public sealed partial class EditTabViewContent : UserControl
{
    public EditTabViewModel ViewModel { get; set; } = new();
    public EditTabViewContent ()
    {
        InitializeComponent();
    }

    public void SetContent (string text , string? fileName)
    {
        var doc = EditBox.Document;
        doc.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
    }
}
