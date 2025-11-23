using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messagers;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InkMD_Editor.ViewModels;

public partial class StoragePickerViewModel
{
    private WindowId GetWindowsId ()
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        return appWindow.Id;
    }

    [RelayCommand]
    private async Task OpenFile ()
    {
        var lastPath = AppSettings.GetLastOpenFolderPath();
        var startLocation = string.IsNullOrEmpty(lastPath)
            ? PickerLocationId.ComputerFolder
            : PickerLocationId.ComputerFolder;
        var picker = new FileOpenPicker(GetWindowsId())
        {
            FileTypeFilter = { ".txt" , ".md" } ,
            SuggestedStartLocation = startLocation ,
        };

        // error cause picker to crash
        //picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var result = await picker.PickSingleFileAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastOpenFolderPath(Path.GetDirectoryName(result.Path) ?? "");

                var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(result.Path);
                WeakReferenceMessenger.Default.Send(new FileOpenedMessage(storageFile));
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Error: {ex.Message}"));
            }
        }
    }

    [RelayCommand]
    private async Task OpenFolder ()
    {
        var lastPath = AppSettings.GetLastFolderPath();
        var startLocation = string.IsNullOrEmpty(lastPath)
            ? PickerLocationId.DocumentsLibrary
            : PickerLocationId.ComputerFolder;
        var picker = new FolderPicker(GetWindowsId())
        {
            SuggestedStartLocation = startLocation,
        };
        var result = await picker.PickSingleFolderAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastFolderPath(result.Path);

                var storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(result.Path);
                WeakReferenceMessenger.Default.Send(new FolderOpenedMessage(storageFolder));
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Lỗi mở folder: {ex.Message}"));
            }
        }
    }

    [RelayCommand]
    private async Task Save ()
    {
        WeakReferenceMessenger.Default.Send(new SaveFileMessage(isNewFile: false));
    }

    [RelayCommand]
    private async Task SaveAsFile ()
    {
        var lastPath = AppSettings.GetLastFolderPath();
        var picker = new FileSavePicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder ,
            DefaultFileExtension = ".md" ,
        };
        picker.FileTypeChoices.Add("Markdown" , new [] { ".md" });
        picker.FileTypeChoices.Add("Text" , new [] { ".txt" });
        var result = await picker.PickSaveFileAsync();
        if ( result is not null )
        {
            try
            {
                AppSettings.SetLastFolderPath(Path.GetDirectoryName(result.Path) ?? "");

                WeakReferenceMessenger.Default.Send(new SaveFileRequestMessage(result.Path));
            }
            catch ( Exception ex )
            {
                WeakReferenceMessenger.Default.Send(new ErrorMessage($"Lỗi lưu file: {ex.Message}"));
            }
        }
    }

    [RelayCommand]
    private void ExitApplication ()
    {
        App.Current.Exit();
    }
}
