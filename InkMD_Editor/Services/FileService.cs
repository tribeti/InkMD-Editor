using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Interfaces;
using InkMD_Editor.Messagers;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Services;

public class FileService : IFileService
{
    /// <summary>
    /// Gets the WindowId for file picker operations.
    /// </summary>
    private static WindowId GetWindowsId ()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        return appWindow.Id;
    }

    /// <summary>
    /// Opens a file picker dialog and returns the selected file.
    /// </summary>
    public async Task<StorageFile?> OpenFileAsync ()
    {
        var startLocation = PickerLocationId.ComputerFolder;
        var picker = new FileOpenPicker(GetWindowsId())
        {
            FileTypeFilter = { ".txt" , ".md" } ,
            SuggestedStartLocation = startLocation ,
        };

        var result = await picker.PickSingleFileAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastOpenFolderPath(Path.GetDirectoryName(result.Path) ?? "");
                var storageFile = await StorageFile.GetFileFromPathAsync(result.Path);
                return storageFile;
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Error: {ex.Message}"));
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Opens a folder picker dialog and returns the selected folder.
    /// </summary>
    public async Task<StorageFolder?> OpenFolderAsync ()
    {
        var startLocation = PickerLocationId.ComputerFolder;
        var picker = new FolderPicker(GetWindowsId())
        {
            SuggestedStartLocation = startLocation ,
        };

        var result = await picker.PickSingleFolderAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastFolderPath(result.Path);
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
                return storageFolder;
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Lỗi mở folder: {ex.Message}"));
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Opens a file save picker dialog and returns the selected file path.
    /// </summary>
    public async Task<string?> SaveFileAsync ()
    {
        var picker = new FileSavePicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder ,
            DefaultFileExtension = ".md" ,
        };
        picker.FileTypeChoices.Add("Markdown" , [".md"]);
        picker.FileTypeChoices.Add("Text" , [".txt"]);

        var result = await picker.PickSaveFileAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastFolderPath(Path.GetDirectoryName(result.Path) ?? "");
                return result.Path;
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Lỗi lưu file: {ex.Message}"));
                return null;
            }
        }
        return null;
    }
}
