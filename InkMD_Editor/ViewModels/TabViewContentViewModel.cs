using CommunityToolkit.Mvvm.ComponentModel;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial string? CurrentContent { get; set; }

    /// <summary>
    /// Check if the file has been saved
    /// </summary>
    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public TabViewContentViewModel ()
    {
        FileName = "Untitled";
    }

    /// <summary>
    /// Set file path and name when saving or opening a file
    /// </summary>
    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    /// <summary>
    /// reset file path and name for a new unsaved file
    /// </summary>
    public void ResetForNewFile ()
    {
        FilePath = null;
        FileName = "New Document";
    }
}
