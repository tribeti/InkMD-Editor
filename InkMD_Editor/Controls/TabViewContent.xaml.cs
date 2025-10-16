using InkMD_Editor.ViewModels;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl
{
    private WordCountViewModel ViewModel { get; set; } = new();
    public TabViewContent ()
    {
        InitializeComponent();
    }

    private void MdEditor_TextChanged (object sender , Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var doc = MdEditor.Document;
        doc.GetText(TextGetOptions.None , out string text);
        ViewModel?.UpdateWordCount(text);
    }
}
