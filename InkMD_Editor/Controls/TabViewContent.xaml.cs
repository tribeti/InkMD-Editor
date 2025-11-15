using InkMD_Editor.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl
{
    public TabViewContentViewModel ViewModel { get; set; } = new();

    public TabViewContent ()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
    }

    private void MdEditor_TextChanged (object sender , RoutedEventArgs e)
    {
        var doc = MdEditor.Document;
        doc.GetText(TextGetOptions.None , out string text);
        ViewModel?.UpdateWordCount(text);
    }

    public void SetContent (string text , string fileName)
    {
        var doc = MdEditor.Document;
        doc.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
        ViewModel.UpdateWordCount(text);
    }
}
