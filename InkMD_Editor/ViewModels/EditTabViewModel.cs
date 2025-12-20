using CommunityToolkit.Mvvm.ComponentModel;
using InkMD_Editor.Helpers;

namespace InkMD_Editor.ViewModels;

public partial class EditTabViewModel : ObservableObject
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

    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public EditTabViewModel ()
    {
        FileName = "Untitled";
        FontFamily = AppSettings.GetFontFamily();
        FontSize = AppSettings.GetFontSize();
    }

    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }
}
