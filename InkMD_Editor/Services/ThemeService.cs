using Microsoft.UI.Xaml;
using System;
using Windows.Storage;

namespace InkMD_Editor.Services;

public static class ThemeService
{
    private const string ThemeSettingKey = "AppTheme";

    public enum AppTheme
    {
        Light = 0,
        Dark = 1,
        Default = 2
    }

    public static void SaveTheme (AppTheme theme)
    {
        ApplicationData.Current.LocalSettings.Values [ThemeSettingKey] = (int) theme;
    }

    public static AppTheme GetSavedTheme ()
    {
        if ( ApplicationData.Current.LocalSettings.Values.TryGetValue(ThemeSettingKey , out object? value) && value is int themeValue )
        {
            if ( Enum.IsDefined(typeof(AppTheme) , themeValue) )
            {
                return (AppTheme) themeValue;
            }
        }
        return AppTheme.Default;
    }

    public static ElementTheme ToElementTheme (AppTheme theme)
    {
        return theme switch
        {
            AppTheme.Light => ElementTheme.Light,
            AppTheme.Dark => ElementTheme.Dark,
            AppTheme.Default => ElementTheme.Default,
            _ => ElementTheme.Default
        };
    }

    public static void ApplyTheme (Window window)
    {
        if ( window?.Content is FrameworkElement rootElement )
        {
            var savedTheme = GetSavedTheme();
            rootElement.RequestedTheme = ToElementTheme(savedTheme);
        }
    }

    public static void SetTheme (Window window , AppTheme theme)
    {
        if ( window?.Content is FrameworkElement rootElement )
        {
            SaveTheme(theme);
            rootElement.RequestedTheme = ToElementTheme(theme);
        }
    }
}