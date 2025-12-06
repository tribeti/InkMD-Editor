using CommunityToolkit.Mvvm.ComponentModel;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    /// <summary>
    /// Kiểm tra xem file đã được lưu chưa
    /// </summary>
    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public TabViewContentViewModel ()
    {
        FileName = "Untitled";
    }

    /// <summary>
    /// Gọi khi file được lưu thành công
    /// </summary>
    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    /// <summary>
    /// Reset khi tạo tab mới
    /// </summary>
    public void ResetForNewFile ()
    {
        FilePath = null;
        FileName = "New Document";
    }
}
