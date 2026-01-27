using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;
using InkMD_Editor.Models;
using InkMD_Editor.Services;
using Markdig;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace InkMD_Editor.ViewModels;

public partial class MainMenuViewModel(IFileService fileService) : ObservableObject
{
    private readonly IFileService _fileService = fileService;
    private readonly MarkdownPipeline _markdownPipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseEmojiAndSmiley()
        .Build();

    [ObservableProperty]
    public partial ObservableCollection<IconItem> IconItems { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<MdTemplate> Templates { get; set; } = [];

    [ObservableProperty]
    public partial List<string> SelectedIcons { get; set; } = [];

    [ObservableProperty]
    public partial string GeneratedIconCode { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IconsLoaded { get; set; }

    [ObservableProperty]
    public partial bool TemplatesLoaded { get; set; }

    [RelayCommand]
    private async Task OpenFileAsync()
    {
        var storageFile = await _fileService.OpenFileAsync();
        if (storageFile is not null)
        {
            WeakReferenceMessenger.Default.Send(new FileOpenedMessage(storageFile));
        }
    }

    [RelayCommand]
    private async Task OpenFolderAsync()
    {
        var storageFolder = await _fileService.OpenFolderAsync();
        if (storageFolder is not null)
        {
            WeakReferenceMessenger.Default.Send(new FolderOpenedMessage(storageFolder));
        }
    }

    [RelayCommand]
    private static void Save()
    {
        WeakReferenceMessenger.Default.Send(new SaveFileMessage(IsNewFile: false));
    }

    [RelayCommand]
    private async Task SaveAsAsync()
    {
        var filePath = await _fileService.SaveFileAsync();
        if (filePath is not null)
        {
            WeakReferenceMessenger.Default.Send(new SaveFileRequestMessage(filePath));
        }
    }

    [RelayCommand]
    private static void ExitApplication() => App.Current.Exit();

    public async Task<bool> CreateFileAsync(string fileName, string extension)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            fileName = "Untitled";
        }

        var storageFile = await _fileService.CreateFileDirectlyAsync(fileName, extension);

        if (storageFile is not null)
        {
            WeakReferenceMessenger.Default.Send(new FileOpenedMessage(storageFile));
            return true;
        }

        return false;
    }

    [RelayCommand]
    private async Task LoadTemplatesAsync()
    {
        if (TemplatesLoaded)
        {
            return;
        }
        var templates = await TemplateService.GetAllTemplatesAsync();
        Templates = new ObservableCollection<MdTemplate>(templates);
        TemplatesLoaded = true;
    }

    public async Task<string?> LoadTemplateContentAsync(string fileName)
    {
        try
        {
            return await TemplateService.LoadTemplateAsync(fileName);
        }
        catch
        {
            return null;
        }
    }

    public void SendTemplateSelectedMessage(string content, bool createNewFile)
    {
        WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content, createNewFile));
    }

    public bool CreateHyperlink(string displayText, string url, out string hyperlinkMarkdown)
    {
        hyperlinkMarkdown = $"[{displayText.Trim()}]({url.Trim()})";
        return true;
    }

    public void SendHyperlinkMessage(string markdown)
    {
        WeakReferenceMessenger.Default.Send(new HyperlinkCreationMessage(markdown));
    }

    [RelayCommand]
    private async Task LoadIconsAsync()
    {
        if (IconsLoaded)
        {
            return;
        }
        var icons = await TemplateService.GetAllIconsAsync();
        IconItems = new ObservableCollection<IconItem>(icons);
        IconsLoaded = true;
    }

    public void AddSelectedIcon(string iconName)
    {
        if (!SelectedIcons.Contains(iconName))
        {
            SelectedIcons.Add(iconName);
            UpdateGeneratedIconCode();
        }
    }

    public void RemoveSelectedIcon(string iconName)
    {
        SelectedIcons.Remove(iconName);
        UpdateGeneratedIconCode();
    }

    public void ClearSelectedIcons()
    {
        SelectedIcons.Clear();
        UpdateGeneratedIconCode();
    }

    private void UpdateGeneratedIconCode()
    {
        GeneratedIconCode = SelectedIcons.Count == 0
            ? "![](https://ink-md-server.vercel.app/api?i=)"
            : $"![](https://ink-md-server.vercel.app/api?i={string.Join(",", SelectedIcons)})";
    }

    public string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return GitHubPreview.GetEmptyPreviewHtml();
        }

        var htmlBody = Markdown.ToHtml(markdown, _markdownPipeline);
        return GitHubPreview.WrapWithGitHubStyle(htmlBody);
    }

    private void SendEditCommand(EditCommandType commandType) => WeakReferenceMessenger.Default.Send(new EditCommandMessage(commandType));

    [RelayCommand]
    private void Undo() => SendEditCommand(EditCommandType.Undo);

    [RelayCommand]
    private void Redo() => SendEditCommand(EditCommandType.Redo);

    [RelayCommand]
    private void Cut() => SendEditCommand(EditCommandType.Cut);

    [RelayCommand]
    private void Copy() => SendEditCommand(EditCommandType.Copy);

    [RelayCommand]
    private void Paste() => SendEditCommand(EditCommandType.Paste);

    [RelayCommand]
    private void Bold() => SendEditCommand(EditCommandType.Bold);

    [RelayCommand]
    private void Italic() => SendEditCommand(EditCommandType.Italic);

    [RelayCommand]
    private void Strikethrough() => SendEditCommand(EditCommandType.Strikethrough);

    public void Cleanup()
    {
        Templates.Clear();
        IconItems.Clear();
        SelectedIcons.Clear();
        TemplatesLoaded = false;
        IconsLoaded = false;
    }
}