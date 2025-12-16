using InkMD_Editor.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

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
        ThemeComboBox.SelectedItem = ThemeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag as string == savedTheme.ToString());
    }

    private void ThemeComboBox_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( ThemeComboBox.SelectedItem is not ComboBoxItem { Tag: string themeTag } )
        {
            return;
        }

        if ( Enum.TryParse<ThemeService.AppTheme>(themeTag , out var selectedTheme) )
        {
            var window = App.MainWindow;
            if ( window is not null )
            {
                ThemeService.SetTheme(window , selectedTheme);
            }
        }
    }

    private void Button_Click (object sender , Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ( Frame.CanGoBack )
        {
            Frame.GoBack();
        }
        else
        {
            Frame.Navigate(typeof(EditorPage));
        }
    }
}
