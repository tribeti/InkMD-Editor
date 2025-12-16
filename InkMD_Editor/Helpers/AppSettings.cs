using Windows.Storage;

namespace InkMD_Editor.Helpers;

public static class AppSettings
{
    private static readonly ApplicationDataContainer _localSettings =
        ApplicationData.Current.LocalSettings;

    private const string LAST_FOLDER_KEY = "LastFolderPath";
    private const string LAST_OPEN_FOLDER_KEY = "LastOpenFolderPath";

    /// <summary>
    /// Get last used folder path
    /// </summary>
    public static string GetLastFolderPath ()
    {
        if ( _localSettings.Values.TryGetValue(LAST_FOLDER_KEY , out var value) )
        {
            return value?.ToString() ?? "";
        }
        return "";
    }

    /// <summary>
    /// Save last used folder path
    /// </summary>
    public static void SetLastFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_FOLDER_KEY] = folderPath;
    }

    /// <summary>
    /// Get last used folder path
    /// </summary>
    public static string GetLastOpenFolderPath ()
    {
        if ( _localSettings.Values.TryGetValue(LAST_OPEN_FOLDER_KEY , out var value) )
        {
            return value?.ToString() ?? "";
        }
        return "";
    }

    /// <summary>
    /// Save last used folder path
    /// </summary>
    public static void SetLastOpenFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_OPEN_FOLDER_KEY] = folderPath;
    }
}