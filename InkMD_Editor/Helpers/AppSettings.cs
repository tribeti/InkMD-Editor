using Windows.Storage;

namespace InkMD_Editor.Helpers;

public static class AppSettings
{
    private static readonly ApplicationDataContainer _localSettings = ApplicationData.Current.LocalSettings;

    private const string LAST_FOLDER_KEY = "LastFolderPath";
    private const string LAST_OPEN_FOLDER_KEY = "LastOpenFolderPath";
    private const string FONT_FAMILY_KEY = "EditorFontFamily";
    private const string FONT_SIZE_KEY = "EditorFontSize";

    public static string GetLastFolderPath ()
    {
        if ( _localSettings.Values.TryGetValue(LAST_FOLDER_KEY , out var value) )
        {
            return value?.ToString() ?? "";
        }
        return "";
    }

    public static void SetLastFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_FOLDER_KEY] = folderPath;
    }

    public static string GetLastOpenFolderPath ()
    {
        if ( _localSettings.Values.TryGetValue(LAST_OPEN_FOLDER_KEY , out var value) )
        {
            return value?.ToString() ?? "";
        }
        return "";
    }

    public static void SetLastOpenFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_OPEN_FOLDER_KEY] = folderPath;
    }

    public static string GetFontFamily ()
    {
        if ( _localSettings.Values.TryGetValue(FONT_FAMILY_KEY , out var value) )
        {
            return value?.ToString() ?? "Cascadia Mono";
        }
        return "Cascadia Mono";
    }

    public static void SetFontFamily (string fontFamily)
    {
        _localSettings.Values [FONT_FAMILY_KEY] = fontFamily;
    }

    public static int GetFontSize ()
    {
        if ( _localSettings.Values.TryGetValue(FONT_SIZE_KEY , out var value) )
        {
            if ( value is int doubleValue )
                return doubleValue;
            if ( int.TryParse(value?.ToString() , out int parsedValue) )
                return parsedValue;
        }
        return 14;
    }

    public static void SetFontSize (double fontSize)
    {
        _localSettings.Values [FONT_SIZE_KEY] = fontSize;
    }
}