using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int? WordCount { get; set; }

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
        WordCount = 0;
        FileName = "Untitled";
    }

    public void UpdateWordCount (string text)
    {
        if ( string.IsNullOrWhiteSpace(text) )
        {
            WordCount = 0;
            return;
        }
        WordCount = System.Text.RegularExpressions.Regex.Matches(text , @"\b\w+\b").Count;
        WeakReferenceMessenger.Default.Send(
            new WordCountMessage { WordCount = this.WordCount }
        );
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
        WordCount = 0;
    }
}
