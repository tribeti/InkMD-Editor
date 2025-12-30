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
    [NotifyPropertyChangedFor(nameof(IsSaved) , nameof(IsDirty))]
    public partial string? CurrentContent { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = AppSettings.GetFontFamily();

    [ObservableProperty]
    public partial double FontSize { get; set; } = AppSettings.GetFontSize();

    [ObservableProperty]
    public partial string Tag { get; set; } = "split";

    [ObservableProperty]
    public partial bool IsLoadingContent { get; set; }

    private bool _lastDirtyState = false;

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

    /// <summary>
    /// Mark the file as clean (no unsaved changes)
    /// </summary>
    public void MarkAsClean ()
    {
        OriginalContent = CurrentContent;
        _lastDirtyState = false;
        OnPropertyChanged(nameof(IsDirty));
        WeakReferenceMessenger.Default.Send(new ContentChangedMessage(
            FilePath ?? string.Empty ,
            false
        ));
    }

    /// <summary>
    /// Set the original content when loading a file
    /// </summary>
    public void SetOriginalContent (string content)
    {
        IsLoadingContent = true;
        OriginalContent = content;
        CurrentContent = content;
        _lastDirtyState = false;
        IsLoadingContent = false;
    }

    /// <summary>
    /// Called when CurrentContent changes - send message if dirty state changed
    /// </summary>
    partial void OnCurrentContentChanged (string? value)
    {
        // Don't check dirty state while loading content
        if ( IsLoadingContent )
            return;

        bool currentDirtyState = IsDirty;

        // Only send message if dirty state actually changed
        if ( currentDirtyState != _lastDirtyState )
        {
            _lastDirtyState = currentDirtyState;
            WeakReferenceMessenger.Default.Send(new ContentChangedMessage(
                FilePath ?? string.Empty ,
                currentDirtyState
            ));
        }
    }

    public void Receive (FontChangedMessage message)
    {
        FontFamily = message.FontFamily;
        FontSize = message.FontSize;
    }
}
