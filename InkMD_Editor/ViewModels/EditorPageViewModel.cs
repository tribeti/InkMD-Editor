using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

namespace InkMD_Editor.ViewModels;

public partial class EditorPageViewModel : ObservableObject
{
    private readonly FileService _fileService = new();
    private DialogService _dialogService = new();

    [ObservableProperty]
    public partial string? RootPath { get; set; }

    public EditorPageViewModel ()
    {
        InitializeRootPath();
    }

    private void InitializeRootPath ()
    {
        RootPath = AppSettings.GetLastFolderPath();
        if ( string.IsNullOrEmpty(RootPath) )
        {
            RootPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        }
    }

    public void SetDialogService (DialogService dialogService)
    {
        _dialogService = dialogService;
    }

    public async Task<TreeViewNode?> InitializeTreeViewAsync ()
    {
        try
        {
            var folder = await StorageFolder.GetFolderFromPathAsync(RootPath!);
            var node = CreateTreeViewNode(folder);
            await FillTreeNodeAsync(node);
            return node;
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"TreeView Init Error: {ex.Message}");
            return null;
        }
    }

    public async Task<TreeViewNode?> RefreshTreeViewWithFolderAsync (StorageFolder folder)
    {
        try
        {
            var node = CreateTreeViewNode(folder);
            await FillTreeNodeAsync(node);
            RootPath = folder.Path;
            return node;
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"TreeView can not refresh: {ex.Message}");
            return null;
        }
    }

    public async Task<bool> FillTreeNodeAsync (TreeViewNode node)
    {
        var folder = GetStorageFolder(node);
        if ( folder is null )
        {
            return false;
        }

        try
        {
            var itemsList = await folder.GetItemsAsync();

            if ( itemsList.Count == 0 )
            {
                node.HasUnrealizedChildren = false;
                return true;
            }

            foreach ( var item in itemsList )
            {
                var newNode = new TreeViewNode
                {
                    Content = item ,
                    HasUnrealizedChildren = item is StorageFolder
                };
                node.Children.Add(newNode);
            }

            node.HasUnrealizedChildren = false;
            return true;
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"Cannot load items: {ex.Message}");
            return false;
        }
    }

    public void CollapseTreeNode (TreeViewNode node)
    {
        node.Children.Clear();
        node.HasUnrealizedChildren = true;
    }

    private static TreeViewNode CreateTreeViewNode (StorageFolder folder) =>
        new()
        {
            Content = folder ,
            IsExpanded = true ,
            HasUnrealizedChildren = true
        };

    private static StorageFolder? GetStorageFolder (TreeViewNode node) =>
        node.Content is StorageFolder && node.HasUnrealizedChildren
            ? node.Content as StorageFolder
            : null;

    public async Task<(string content, string fileName, string filePath)?> OpenFileAsync (StorageFile file)
    {
        try
        {
            var text = await ReadFileTextAsync(file);
            return (text, file.Name, file.Path);
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"Cannot open file: {ex.Message}");
            return null;
        }
    }

    public async Task HandleSaveFile (TabViewContent? content)
    {
        if ( content is null )
        {
            await ShowErrorAsync("Cannot get tab content");
            return;
        }

        var viewModel = content.ViewModel;
        var filePath = !string.IsNullOrEmpty(viewModel.FilePath)
            ? viewModel.FilePath
            : await _fileService.SaveFileAsync();

        if ( filePath is not null )
        {
            await SaveFileToPath(filePath , content);
        }
    }

    public async Task SaveFileToPath (string filePath , TabViewContent content)
    {
        try
        {
            string editorText = content.GetContent();
            await File.WriteAllTextAsync(filePath , editorText , Encoding.UTF8);

            var viewModel = content.ViewModel;
            viewModel.SetFilePath(filePath , Path.GetFileName(filePath));

            await ShowSuccessAsync($"Saved: {Path.GetFileName(filePath)}");
            WeakReferenceMessenger.Default.Send(new FileSavedMessage(filePath , Path.GetFileName(filePath)));
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"Save file error: {ex.Message}");
        }
    }

    public (bool success, string? content, string? error) CreateNewTabContent (string templateContent , int tabCount)
    {
        return (true, templateContent, null);
    }

    public async Task<(bool success, string? newContent, string? error)> InsertIntoDocumentAsync (string templateContent , TabViewContent? tabContent)
    {
        if ( tabContent is null )
        {
            return (false, null, "Cannot access current documents.");
        }

        try
        {
            var currentContent = tabContent.GetContent();
            var newContent = string.IsNullOrWhiteSpace(currentContent)
                ? templateContent
                : currentContent + "\n\n" + templateContent;

            return (true, newContent, null);
        }
        catch ( Exception ex )
        {
            return (false, null, $"Cannot insert template: {ex.Message}");
        }
    }

    public async Task<string> ReadFileTextAsync (StorageFile file)
    {
        try
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            var bytes = BufferToBytes(buffer);
            return DetectAndDecodeBytes(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static byte [] BufferToBytes (IBuffer buffer)
    {
        using var dataReader = DataReader.FromBuffer(buffer);
        var bytes = new byte [buffer.Length];
        dataReader.ReadBytes(bytes);
        return bytes;
    }

    private static string DetectAndDecodeBytes (ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> utf8Bom = [0xEF , 0xBB , 0xBF];
        ReadOnlySpan<byte> utf16LeBom = [0xFF , 0xFE];
        ReadOnlySpan<byte> utf16BeBom = [0xFE , 0xFF];

        if ( bytes.Length >= 3 && bytes [..3].SequenceEqual(utf8Bom) )
        {
            return Encoding.UTF8.GetString(bytes [3..]);
        }

        if ( bytes.Length >= 2 && bytes [..2].SequenceEqual(utf16LeBom) )
        {
            return Encoding.Unicode.GetString(bytes [2..]);
        }

        if ( bytes.Length >= 2 && bytes [..2].SequenceEqual(utf16BeBom) )
        {
            return Encoding.BigEndianUnicode.GetString(bytes [2..]);
        }

        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return Encoding.Default.GetString(bytes);
        }
    }

    public bool IsMarkdownFile (StorageFile? file) => file?.FileType.Equals(".md" , StringComparison.OrdinalIgnoreCase) ?? false;

    public async Task ShowErrorAsync (string message) => await _dialogService.ShowErrorAsync(message);

    public async Task ShowSuccessAsync (string message) => await _dialogService.ShowSuccessAsync(message);

}