using Windows.Storage;

namespace InkMD_Editor.Helpers;

public static class AppSettings
{
    private static readonly ApplicationDataContainer _localSettings =
        ApplicationData.Current.LocalSettings;

    private const string LAST_FOLDER_KEY = "LastFolderPath";
    private const string LAST_OPEN_FOLDER_KEY = "LastOpenFolderPath";

    /// <summary>
    /// Lấy folder được sử dụng lần cuối
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
    /// Lưu folder được sử dụng
    /// </summary>
    public static void SetLastFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_FOLDER_KEY] = folderPath;
    }

    /// <summary>
    /// Lấy folder Open được sử dụng lần cuối
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
    /// Lưu folder Open được sử dụng
    /// </summary>
    public static void SetLastOpenFolderPath (string folderPath)
    {
        _localSettings.Values [LAST_OPEN_FOLDER_KEY] = folderPath;
    }
}