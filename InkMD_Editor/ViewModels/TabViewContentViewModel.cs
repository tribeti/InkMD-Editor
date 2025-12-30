using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject, IRecipient<FontChangedMessage>
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    public partial string? OriginalContent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaved),nameof(IsDirty))]
    public partial string? CurrentContent { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = AppSettings.GetFontFamily();

    [ObservableProperty]
    public partial double FontSize { get; set; } = AppSettings.GetFontSize();

    [ObservableProperty]
    public partial string Tag { get; set; } = "split";

    /// <summary>
    /// Check if the file has been saved
    /// </summary>
    public bool IsSaved => !string.IsNullOrEmpty(FilePath);
    
    /// <summary>
    /// Check if the file has unsaved changes
    /// </summary>
    public bool IsDirty => OriginalContent != CurrentContent;

    public TabViewContentViewModel ()
    {
        FileName = "Untitled";
        WeakReferenceMessenger.Default.Register(this);
    }

    /// <summary>
    /// Set file path and name when saving or opening a file
    /// </summary>
    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    public void Receive (FontChangedMessage message)
    {
        FontFamily = message.FontFamily;
        FontSize = message.FontSize;
    }

    /// <summary>
    /// Mark the file as clean (no unsaved changes)
    /// </summary>
    public void MarkAsClean ()
    {
        OriginalContent = CurrentContent;
        OnPropertyChanged(nameof(IsDirty));
    }

    /// <summary>
    /// Set the original content when loading a file
    /// </summary>
    public void SetOriginalContent (string content)
    {
        OriginalContent = content;
        CurrentContent = content;
    }
}
