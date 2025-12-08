using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.ViewModels;

public partial class EditorPageViewModel : ObservableObject
{
    private readonly FileService _fileService = new();
    private DialogService _dialogService = new();

    [ObservableProperty]
    public partial string RootPath { get; set; }

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

    /// <summary>
    /// Handles save file operation - saves to existing path or prompts for new path
    /// </summary>
    public async Task HandleSaveFile (TabViewContent? content)
    {
        if ( content is null )
        {
            await ShowErrorAsync("Không thể lấy nội dung tab");
            return;
        }

        var viewModel = content.ViewModel;
        if ( !string.IsNullOrEmpty(viewModel.FilePath) )
        {
            await SaveFileToPath(viewModel.FilePath , content);
        }
        else
        {
            var filePath = await _fileService.SaveFileAsync();
            if ( filePath is not null )
            {
                await SaveFileToPath(filePath , content);
            }
        }
    }

    /// <summary>
    /// Saves file content to the specified path
    /// </summary>
    public async Task SaveFileToPath (string filePath , TabViewContent content)
    {
        try
        {
            string editorText = content.GetContent();
            await File.WriteAllTextAsync(filePath , editorText , Encoding.UTF8);

            var viewModel = content.ViewModel;
            viewModel.SetFilePath(filePath , Path.GetFileName(filePath));

            await ShowSuccessAsync($"Đã lưu: {Path.GetFileName(filePath)}");
            WeakReferenceMessenger.Default.Send(new FileSavedMessage(filePath , Path.GetFileName(filePath)));
        }
        catch ( Exception ex )
        {
            await ShowErrorAsync($"Lỗi lưu file: {ex.Message}");
        }
    }

    /// <summary>
    /// Reads file content with proper encoding detection
    /// </summary>
    public async Task<string> ReadFileTextAsync (StorageFile file)
    {
        try
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            byte [] bytes;
            using ( var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer) )
            {
                bytes = new byte [buffer.Length];
                dataReader.ReadBytes(bytes);
            }

            return DetectAndDecodeBytes(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }

    /// <summary>
    /// Detects file encoding from BOM and decodes bytes accordingly
    /// </summary>
    private static string DetectAndDecodeBytes (byte [] bytes)
    {
        // UTF-8 with BOM
        if ( bytes.Length >= 3 && bytes [0] == 0xEF && bytes [1] == 0xBB && bytes [2] == 0xBF )
        {
            return Encoding.UTF8.GetString(bytes , 3 , bytes.Length - 3);
        }

        // UTF-16 LE
        if ( bytes.Length >= 2 && bytes [0] == 0xFF && bytes [1] == 0xFE )
        {
            return Encoding.Unicode.GetString(bytes , 2 , bytes.Length - 2);
        }

        // UTF-16 BE
        if ( bytes.Length >= 2 && bytes [0] == 0xFE && bytes [1] == 0xFF )
        {
            return Encoding.BigEndianUnicode.GetString(bytes , 2 , bytes.Length - 2);
        }

        // No BOM — try UTF-8 first, then UTF-16, then fall back to system/default encoding
        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch { }

        try
        {
            return Encoding.Unicode.GetString(bytes);
        }
        catch { }

        return Encoding.Default.GetString(bytes);
    }

    public async Task ShowErrorAsync (string message) => await _dialogService.ShowErrorAsync(message);

    public async Task ShowSuccessAsync (string message) => await _dialogService.ShowSuccessAsync(message);
}
