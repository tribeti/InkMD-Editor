using CommunityToolkit.Mvvm.Input;
using Microsoft.UI;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.IO;
using System.Threading.Tasks;

namespace InkMD_Editor.ViewModels;

public partial class MenuBarViewModel
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
        var picker = new FileOpenPicker(GetWindowsId())
        {
            FileTypeFilter = { ".txt" , ".md" } ,
            SuggestedStartLocation = PickerLocationId.ComputerFolder ,
        };

        // error cause picker to crash
        //picker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;

        var result = await picker.PickSingleFileAsync();
        if ( result != null )
        {
            // Perform this conversion if you have business logic that uses StorageFile
            var storageFile = await Windows.Storage.StorageFile.GetFileFromPathAsync(result.Path);
        }
        else
        {
            // Add error handling logic here
        }
    }

    [RelayCommand]
    private async Task OpenFolder ()
    {
        var picker = new FolderPicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder ,
        };
        var result = await picker.PickSingleFolderAsync();
        if ( result != null )
        {
            var storageFolder = await Windows.Storage.StorageFolder.GetFolderFromPathAsync(result.Path);
        }
        else
        {
            // Add error handling logic here
        }
    }

    [RelayCommand]
    private async Task SaveFile ()
    {
        var picker = new FileSavePicker(GetWindowsId())
        {
            SuggestedStartLocation = PickerLocationId.ComputerFolder ,
            DefaultFileExtension = ".md" ,
        };
        var result = await picker.PickSaveFileAsync();
        if ( result != null )
        {
            string savePath = result.Path;
            await File.WriteAllTextAsync(savePath , "# Hello World");
        }
        else
        {
            // Add error handling logic here
        }
    }

    [RelayCommand]
    private void ExitApplication ()
    {
        App.Current.Exit();
    }
}
