using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;
using InkMD_Editor.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;

namespace InkMD_Editor.Views;

public sealed partial class SettingsPage : Page
{
    private readonly ThemeService _themeService;

    public SettingsPage ()
    {
        InitializeComponent();
        var app = (App) Application.Current;
        _themeService = app.Services.GetRequiredService<ThemeService>();

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
        var savedTheme = _themeService.GetSavedTheme();
        ThemeComboBox.SelectedItem = ThemeComboBox.Items.OfType<ComboBoxItem>().FirstOrDefault(item => item.Tag as string == savedTheme.ToString());
    }

    private void ThemeComboBox_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( ThemeComboBox.SelectedItem is not ComboBoxItem { Tag: string themeTag } )
            return;

        if ( !Enum.TryParse<ThemeService.AppTheme>(themeTag , out var selectedTheme) )
            return;

        if ( App.MainWindow is Window window )
        {
            _themeService.SetTheme(window , selectedTheme);
        }
    }

    private void FontFamilyComboBox_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( FontFamilyComboBox.SelectedItem is string fontFamily )
        {
            AppSettings.SetFontFamily(fontFamily);
            FontSettingsChanged();
        }
    }

    private void FontSizeBox_ValueChanged (NumberBox sender , NumberBoxValueChangedEventArgs args)
    {
        if ( args.NewValue >= 8 && args.NewValue <= 72 )
        {
            AppSettings.SetFontSize(args.NewValue);
            FontSettingsChanged();
        }
    }

    private void FontSettingsChanged ()
    {
        var fontFamily = AppSettings.GetFontFamily();
        var fontSize = AppSettings.GetFontSize();

        WeakReferenceMessenger.Default.Send(new FontChangedMessage(fontFamily , fontSize));
    }

    private void Button_Click (object sender , RoutedEventArgs e)
    {
        if ( Frame.CanGoBack )
            Frame.GoBack();
        else
            Frame.Navigate(typeof(EditorPage));
    }
}
