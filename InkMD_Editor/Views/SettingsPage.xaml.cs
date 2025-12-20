using InkMD_Editor.Helpers;
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
        LoadSavedFontAndSize();
    }

    private void LoadSavedFontAndSize ()
    {
        var savedFontFamily = AppSettings.GetFontFamily();
        FontFamilyComboBox.SelectedItem = savedFontFamily;

        var savedFontSize = AppSettings.GetFontSize();
        FontSizeBox.Value = savedFontSize;
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

    private void FontFamilyComboBox_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( FontFamilyComboBox.SelectedItem is string fontFamily )
        {
            AppSettings.SetFontFamily(fontFamily);
            ApplyFontSettingsToAllTabs();
        }
    }

    private void FontSizeBox_ValueChanged (NumberBox sender , NumberBoxValueChangedEventArgs args)
    {
        if ( args.NewValue >= 8 && args.NewValue <= 72 )
        {
            AppSettings.SetFontSize(args.NewValue);
            ApplyFontSettingsToAllTabs();
        }
    }

    private void ApplyFontSettingsToAllTabs ()
    {
        var fontFamily = AppSettings.GetFontFamily();
        var fontSize = AppSettings.GetFontSize();

        if ( App.MainWindow?.Content is Frame mainFrame )
        {
            if ( mainFrame.Content is EditorPage editorPage )
            {
                ApplyFontToEditorPage(editorPage , fontFamily , fontSize);
            }
        }
    }

    private void ApplyFontToEditorPage (EditorPage editorPage , string fontFamily , int fontSize)
    {
        if ( editorPage.Content is Grid grid )
        {
            foreach ( var child in grid.Children )
            {
                if ( child is TabView tabView )
                {
                    foreach ( var tab in tabView.TabItems )
                    {
                        if ( tab is TabViewItem tabItem && tabItem.Content is Interfaces.IEditableContent content )
                        {
                            var viewModel = (dynamic) ( (dynamic) content ).ViewModel;
                            if ( viewModel is not null )
                            {
                                viewModel.FontFamily = fontFamily;
                                viewModel.FontSize = fontSize;
                            }
                        }
                    }
                }
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
