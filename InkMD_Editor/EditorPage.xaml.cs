using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor;

public sealed partial class EditorPage : Page
{
    public EditorPage()
    {
        InitializeComponent();
    }

    private void Button_Click (object sender , RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }
}
