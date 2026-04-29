using InkMD.App.Helpers;
using InkMD.Core.Messages;
using InkMD.Core.Services;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace InkMD.App.Services;

public class FileService : IFileService
{
    private const string FolderToken = "CurrentOpenFolder";

    /// <summary>
    /// Gets the WindowId for file picker operations.
    /// </summary>
    private static WindowId GetWindowsId()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        return appWindow.Id;
    }

    /// <summary>
    /// Opens a file picker dialog and returns the selected file.
    /// </summary>
    public async Task<StorageFile?> OpenFileAsync()
    {
        var startLocation = PickerLocationId.ComputerFolder;
        var picker = new FileOpenPicker(GetWindowsId())
        {
            FileTypeFilter = { ".md", "*" },
            SuggestedStartLocation = startLocation,
        };


        var result = await picker.PickSingleFileAsync();
        if (result is not null)
        {
            try
            {
                AppSettings.SetLastOpenFolderPath(Path.GetDirectoryName(result.Path) ?? "");
                var storageFile = await StorageFile.GetFileFromPathAsync(result.Path);
                return storageFile;
            }
            catch (Exception ex)
            {
                RxMessageBus.Default.Publish(new ErrorMessage($"Can not open file: {ex.Message}"));
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Opens a folder picker dialog and returns the selected folder.
    /// </summary>
    public async Task<StorageFolder?> OpenFolderAsync()
    {
        var startLocation = PickerLocationId.ComputerFolder;
        var picker = new FolderPicker(GetWindowsId())
        {
            SuggestedStartLocation = startLocation,
        };
        var result = await picker.PickSingleFolderAsync();

        if (result is not null)
        {
            try
            {
                var storageFolder = await StorageFolder.GetFolderFromPathAsync(result.Path);
                StorageApplicationPermissions.FutureAccessList.AddOrReplace(FolderToken, storageFolder);
                AppSettings.SetLastFolderPath(result.Path);
                return storageFolder;
            }
            catch (Exception ex)
            {
                RxMessageBus.Default.Publish(new ErrorMessage($"Cannot open folder: {ex.Message}"));
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Opens a file save picker dialog and returns the selected file path.
    /// </summary>
    public async Task<string?> SaveFileAsync()
    {
        var picker = new FileSavePicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            DefaultFileExtension = ".md",
        };
        picker.FileTypeChoices.Add("Markdown", [".md"]);
        picker.FileTypeChoices.Add("Text", [".txt"]);
        picker.FileTypeChoices.Add("All Files", ["*"]);

        var result = await picker.PickSaveFileAsync();
        if (result is not null)
        {
            try
            {
                AppSettings.SetLastFolderPath(Path.GetDirectoryName(result.Path) ?? "");
                return result.Path;
            }
            catch (Exception ex)
            {
                RxMessageBus.Default.Publish(new ErrorMessage($"Cannot save file: {ex.Message}"));
                return null;
            }
        }
        return null;
    }

    public async Task<StorageFile?> CreateFileDirectlyAsync(string fileName, string extension)
    {
        string finalFileName = fileName.Trim();
        string safeExtension = extension?.Trim() ?? string.Empty;
        if (!string.IsNullOrEmpty(safeExtension))
        {
            if (!safeExtension.StartsWith("."))
                safeExtension = "." + safeExtension;

            if (!finalFileName.EndsWith(safeExtension, StringComparison.OrdinalIgnoreCase))
            {
                finalFileName += safeExtension;
            }
        }

        char[] invalidChars = Path.GetInvalidFileNameChars();

        if (finalFileName.IndexOfAny(invalidChars) >= 0)
        {
            RxMessageBus.Default.Publish(new ErrorMessage($"File name contains invalid characters. Avoid using: {string.Join(" ", invalidChars)}"));
            return null;
        }
        StorageFolder? currentFolder = null;
        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(FolderToken))
        {
            try
            {
                currentFolder = await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(FolderToken);
            }
            catch
            {
                StorageApplicationPermissions.FutureAccessList.Remove(FolderToken);
            }
        }
        if (currentFolder is null)
        {
            return await CreateNewFileAsync(fileName, extension);
        }

        try
        {
            return await currentFolder.CreateFileAsync(finalFileName, CreationCollisionOption.GenerateUniqueName);
        }
        catch (Exception ex)
        {
            RxMessageBus.Default.Publish(new ErrorMessage($"Cannot create file: {ex.Message}"));
            return null;
        }
    }

    public async Task<StorageFile?> CreateNewFileAsync(string suggestedName, string? extension)
    {
        var picker = new FileSavePicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder,
            SuggestedFileName = suggestedName
        };

        if (!string.IsNullOrEmpty(extension))
        {
            if (!extension.StartsWith("."))
                extension = "." + extension;

            picker.SuggestedFileName = suggestedName + extension;
            picker.DefaultFileExtension = extension;
            string fileTypeName = extension.TrimStart('.').ToUpper() + " File";
            picker.FileTypeChoices.Add(fileTypeName, [extension]);
        }
        else
        {
            picker.SuggestedFileName = suggestedName;
            picker.DefaultFileExtension = string.Empty;
        }

        picker.FileTypeChoices.Add("All Files", ["*"]);

        var result = await picker.PickSaveFileAsync();
        if (result is not null)
        {
            try
            {
                var file = await StorageFile.GetFileFromPathAsync(result.Path);
                AppSettings.SetLastFolderPath(Path.GetDirectoryName(file.Path) ?? "");
                return file;
            }
            catch (Exception ex)
            {
                RxMessageBus.Default.Publish(new ErrorMessage($"File creation error: {ex.Message}"));
                return null;
            }
        }
        return null;
    }
}
