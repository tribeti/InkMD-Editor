using CommunityToolkit.Mvvm.ComponentModel;
using InkMD_Editor.Helpers;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial string? CurrentContent { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = AppSettings.GetFontFamily();

    [ObservableProperty]
    public partial int FontSize { get; set; } = AppSettings.GetFontSize();

    /// <summary>
    /// Check if the file has been saved
    /// </summary>
    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public TabViewContentViewModel ()
    {
        FileName = "Untitled";
        FontFamily = AppSettings.GetFontFamily();
        FontSize = AppSettings.GetFontSize();
    }

    /// <summary>
    /// Set file path and name when saving or opening a file
    /// </summary>
    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }
}
