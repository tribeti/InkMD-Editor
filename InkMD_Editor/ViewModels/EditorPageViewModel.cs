using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;
using InkMD_Editor.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InkMD_Editor.ViewModels;

public partial class EditorPageViewModel(IFileService fileService, IDialogService dialogService) : ObservableObject
{
    private readonly IFileService _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
    private readonly IDialogService _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));

    [ObservableProperty]
    public partial string? RootPath { get; set; }

    public void Initialize()
    {
        RootPath = AppSettings.GetLastFolderPath() is { Length: > 0 } path
            ? path
            : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }

    public async Task<TreeViewNode?> InitializeTreeViewAsync()
    {
        if (string.IsNullOrEmpty(RootPath))
            Initialize();

        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(RootPath!);
            var node = CreateTreeViewNode(folder);
            await FillTreeNodeAsync(node);
            return node;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"TreeView Init Error: {ex.Message}");
            return null;
        }
    }

    public async Task<TreeViewNode?> RefreshTreeViewWithFolderAsync(StorageFolder folder)
    {
        try
        {
            var node = CreateTreeViewNode(folder);
            await FillTreeNodeAsync(node);
            RootPath = folder.Path;
            return node;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Cannot refresh tree: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> FillTreeNodeAsync(TreeViewNode node)
    {
        if (GetStorageFolder(node) is not { } folder)
            return false;

        try
        {
            var itemsList = await folder.GetItemsAsync();
            if (itemsList.Count == 0)
            {
                node.HasUnrealizedChildren = false;
                return true;
            }

            foreach (var item in itemsList)
            {
                node.Children.Add(new TreeViewNode
                {
                    Content = item,
                    HasUnrealizedChildren = item is StorageFolder
                });
            }

            node.HasUnrealizedChildren = false;
            return true;
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Cannot load items: {ex.Message}");
            return false;
        }
    }

    public void CollapseTreeNode(TreeViewNode node)
    {
        node.Children.Clear();
        node.HasUnrealizedChildren = true;
    }

    private static TreeViewNode CreateTreeViewNode(StorageFolder folder) => new()
    {
        Content = folder,
        IsExpanded = true,
        HasUnrealizedChildren = true
    };

    private static StorageFolder? GetStorageFolder(TreeViewNode node) =>
        node is { Content: StorageFolder folder, HasUnrealizedChildren: true } ? folder : null;

    public async Task<(string content, string fileName, string filePath)?> OpenFileAsync(StorageFile file)
    {
        try
        {
            var text = await ReadFileTextAsync(file);
            return (text, file.Name, file.Path);
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Cannot open file: {ex.Message}");
            return null;
        }
    }

    public async Task HandleSaveFile(IEditableContent? content)
    {
        if (content is null)
        {
            await ShowErrorAsync("Cannot get tab content");
            return;
        }

        var filePath = content.GetFilePath();
        if (string.IsNullOrEmpty(filePath))
        {
            filePath = await _fileService.SaveFileAsync();
        }

        if (filePath is not null)
        {
            await SaveFileToPath(filePath, content);
        }
    }

    public async Task SaveFileToPath(string filePath, IEditableContent content)
    {
        try
        {
            await File.WriteAllLinesAsync(filePath, content.GetContentToSaveFile(), Encoding.UTF8);
            var fileName = Path.GetFileName(filePath);

            content.SetFilePath(filePath, fileName);
            WeakReferenceMessenger.Default.Send(new FileSavedMessage(filePath, fileName));
        }
        catch (Exception ex)
        {
            await ShowErrorAsync($"Save file error: {ex.Message}");
        }
    }

    public (bool success, string? content, string? error) CreateNewTabContent(string templateContent, int tabCount) => (true, templateContent, null);

    public async Task<(bool success, string? newContent, string? error)> InsertIntoDocumentAsync(string templateContent, IEditableContent? tabContent)
    {
        if (tabContent is null)
            return (false, null, "Cannot access current documents.");

        try
        {
            var currentContent = tabContent.GetContent();
            var newContent = string.IsNullOrWhiteSpace(currentContent)
                ? templateContent
                : $"{currentContent}\n\n{templateContent}";

            return (true, newContent, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Cannot insert template: {ex.Message}");
        }
    }

    public async Task<string> ReadFileTextAsync(StorageFile file)
    {
        try
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            using var dataReader = DataReader.FromBuffer(buffer);
            var bytes = new byte[buffer.Length];
            dataReader.ReadBytes(bytes);
            return DetectAndDecodeBytes(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string DetectAndDecodeBytes(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> utf8Bom = [0xEF, 0xBB, 0xBF];
        ReadOnlySpan<byte> utf16LeBom = [0xFF, 0xFE];
        ReadOnlySpan<byte> utf16BeBom = [0xFE, 0xFF];

        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(utf8Bom))
            return Encoding.UTF8.GetString(bytes[3..]);

        if (bytes.Length >= 2 && bytes[..2].SequenceEqual(utf16LeBom))
            return Encoding.Unicode.GetString(bytes[2..]);

        if (bytes.Length >= 2 && bytes[..2].SequenceEqual(utf16BeBom))
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);

        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return Encoding.Default.GetString(bytes);
        }
    }

    public bool IsMarkdownFile(StorageFile? file) => file?.FileType.Equals(".md", StringComparison.OrdinalIgnoreCase) ?? false;

    public Task ShowErrorAsync(string message) => _dialogService.ShowErrorAsync(message);
    public Task ShowSuccessAsync(string message) => _dialogService.ShowSuccessAsync(message);
    public Task<bool> ShowConfirmationAsync(string message) => _dialogService.ShowConfirmationAsync(message);
}