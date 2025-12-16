using InkMD_Editor.Services;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Views;

public sealed partial class SettingsPage : Page
{
    public SettingsPage ()
    {
        InitializeComponent();
        LoadSavedTheme();
    }

    private void LoadSavedTheme ()
    {
        var savedTheme = ThemeService.GetSavedTheme();
        ThemeComboBox.SelectedIndex = (int) savedTheme;
    }

    private void ThemeComboBox_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( ThemeComboBox.SelectedIndex == -1 )
            return;

        var selectedTheme = (ThemeService.AppTheme) ThemeComboBox.SelectedIndex;
        var window = App.MainWindow;
        if ( window is not null )
        {
            ThemeService.SetTheme(window , selectedTheme);
        }
    }

    private void Button_Click (object sender , Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(EditorPage));
    }
}
