using CommunityToolkit.Mvvm.ComponentModel;
using InkMD.App.Helpers;
using InkMD.Core.Messages;
using InkMD.Core.Services;
using System;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject, IDisposable
{
    private readonly IDisposable _subscription;

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
    public partial string Tag { get; set; } = "split";

    [ObservableProperty]
    public partial bool IsLoadingContent { get; set; }

    [ObservableProperty]
    public partial bool IsBoldActive { get; set; }

    [ObservableProperty]
    public partial bool IsItalicActive { get; set; }

    [ObservableProperty]
    public partial bool IsStrikethroughActive { get; set; }

    private bool _lastDirtyState = false;

    /// <summary>
    /// Check if the file has been saved
    /// </summary>
    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    /// <summary>
    /// Check if the file has unsaved changes
    /// </summary>
    public bool IsDirty => OriginalContent != CurrentContent;

    public TabViewContentViewModel()
    {
        FileName = "Untitled";
        _subscription = RxMessageBus.Default.Subscribe<FontChangedMessage>().Subscribe(Receive);
    }

    /// <summary>
    /// Set file path and name when saving or opening a file
    /// </summary>
    public void SetFilePath(string path, string name)
    {
        FilePath = path;
        FileName = name;
    }

    /// <summary>
    /// Mark the file as clean (no unsaved changes)
    /// </summary>
    public void MarkAsClean()
    {
        OriginalContent = CurrentContent;
        _lastDirtyState = false;

        RxMessageBus.Default.Publish(new ContentChangedMessage(FilePath ?? string.Empty, false));
    }

    /// <summary>
    /// Set the original content when loading a file (internal use)
    /// Note: IsLoadingContent flag should be managed by the caller
    /// </summary>
    public void SetOriginalContent(string content)
    {
        OriginalContent = content;
        CurrentContent = content;
        _lastDirtyState = false;
    }

    /// <summary>
    /// Called when CurrentContent changes - send message if dirty state changed
    /// </summary>
    partial void OnCurrentContentChanged(string? value)
    {
        if (IsLoadingContent)
            return;

        bool currentDirtyState = IsDirty;

        if (currentDirtyState != _lastDirtyState)
        {
            _lastDirtyState = currentDirtyState;
            RxMessageBus.Default.Publish(new ContentChangedMessage(FilePath ?? string.Empty, currentDirtyState));
        }
    }

    public void Receive(FontChangedMessage message)
    {
        FontFamily = message.FontFamily;
        FontSize = message.FontSize;
    }

    public void Dispose()
    {
        _subscription?.Dispose();
        GC.SuppressFinalize(this);
    }
}