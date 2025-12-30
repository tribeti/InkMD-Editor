using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;

namespace InkMD_Editor.ViewModels;

public partial class EditTabViewModel : ObservableObject, IRecipient<FontChangedMessage>
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSaved))]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    public partial string? OriginalContent { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsDirty))]
    public partial string? CurrentContent { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = AppSettings.GetFontFamily();

    [ObservableProperty]
    public partial double FontSize { get; set; } = AppSettings.GetFontSize();

    [ObservableProperty]
    public partial bool IsLoadingContent { get; set; }

    private bool _lastDirtyState = false;

    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public bool IsDirty => OriginalContent != CurrentContent;

    public EditTabViewModel ()
    {
        FileName = "Untitled";
        WeakReferenceMessenger.Default.Register(this);
    }

    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    public void MarkAsClean ()
    {
        OriginalContent = CurrentContent;
        _lastDirtyState = false;

        WeakReferenceMessenger.Default.Send(new ContentChangedMessage(FilePath ?? string.Empty , false));
    }

    public void SetOriginalContent (string content)
    {
        OriginalContent = content;
        CurrentContent = content;
        _lastDirtyState = false;
    }

    partial void OnCurrentContentChanged (string? value)
    {
        if ( IsLoadingContent )
            return;

        bool currentDirtyState = IsDirty;
        if ( currentDirtyState != _lastDirtyState )
        {
            _lastDirtyState = currentDirtyState;
            WeakReferenceMessenger.Default.Send(new ContentChangedMessage(FilePath ?? string.Empty , currentDirtyState));
        }
    }

    public void Receive (FontChangedMessage message)
    {
        FontFamily = message.FontFamily;
        FontSize = message.FontSize;
    }
}
