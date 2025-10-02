using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor;

public sealed partial class SettingsPage : Page
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private void Button_Click (object sender , Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(EditorPage));
    }
}
