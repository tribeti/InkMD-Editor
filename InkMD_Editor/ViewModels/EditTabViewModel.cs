using CommunityToolkit.Mvvm.ComponentModel;

namespace InkMD_Editor.ViewModels;

public partial class EditTabViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial string? CurrentContent { get; set; }

    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public EditTabViewModel ()
    {
        FileName = "Untitled";
    }

    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    public void ResetForNewFile ()
    {
        FilePath = null;
        FileName = "New Document";
    }
}
