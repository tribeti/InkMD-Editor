using InkMD_Editor.Controls;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.Storage.Pickers;
using System;
using System.Collections.ObjectModel;
using System.IO;

namespace InkMD_Editor;

public sealed partial class EditorPage : Page
{
    public ObservableCollection<ExplorerItem> DataSource { get; set; }
    public WordCountViewModel? ViewModel { get; } = new();
    public EditorPage ()
    {
        InitializeComponent();
        Loaded += EditorPage_Loaded;
        DataSource = GetData();
    }

    private ObservableCollection<ExplorerItem> GetData ()
    {
        return new ObservableCollection<ExplorerItem>
            {
                new ExplorerItem
                {
                    Name = "Documents",
                    Type = ExplorerItem.ExplorerItemType.Folder,
                    Children =
                    {
                        new ExplorerItem
                        {
                            Name = "ProjectProposal",
                            Type = ExplorerItem.ExplorerItemType.File,
                        },
                        new ExplorerItem
                        {
                            Name = "BudgetReport",
                            Type = ExplorerItem.ExplorerItemType.File,
                        },
                    },
                },
                new ExplorerItem
                {
                    Name = "Projects",
                    Type = ExplorerItem.ExplorerItemType.Folder,
                    Children =
                    {
                        new ExplorerItem
                        {
                            Name = "Project Plan",
                            Type = ExplorerItem.ExplorerItemType.File,
                        },
                    },
                },
            };
    }

    private void Button_Click (object sender , RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void EditorPage_Loaded (object sender , RoutedEventArgs e)
    {
        if ( Tabs.TabItems.Count == 0 )
        {
            for ( int i = 0 ; i < 2 ; i++ )
            {
                Tabs.TabItems.Add(CreateNewTab(i));
            }
        }
        if ( Tabs.TabItems.Count > 0 )
        {
            Tabs.SelectedIndex = 0;
        }
    }

    private void TabView_AddButtonClick (TabView sender , object args)
    {
        var newTab = CreateNewTab(sender.TabItems.Count);
        sender.TabItems.Add(newTab);
    }

    private void TabView_TabCloseRequested (TabView sender , TabViewTabCloseRequestedEventArgs args)
    {
        sender.TabItems.Remove(args.Tab);
    }

    private TabViewItem CreateNewTab (int index)
    {
        TabViewItem newItem = new TabViewItem
        {
            Header = $"Document {index}" ,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document }
        };

        var content = new TabViewContent();
        var viewModel = (WordCountViewModel) content.DataContext;
        //viewModel.FileName = $"Document {index}";

        newItem.Content = content;
        return newItem;
    }

    private async void OpenFile_Click (object sender , RoutedEventArgs e)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);

        var picker = new FileOpenPicker(appWindow.Id)
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

    private async void OpenFolder_Click (object sender , RoutedEventArgs e)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        var picker = new FolderPicker(appWindow.Id)
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

    private async void Save_Click (object sender , RoutedEventArgs e)
    {
        var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
        Microsoft.UI.WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
        Microsoft.UI.Windowing.AppWindow appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(windowId);
        var picker = new FileSavePicker(appWindow.Id)
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

    private void Exit_Click (object sender , RoutedEventArgs e)
    {
        App.MainWindow?.Close();
    }

    private void Tabs_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        // Update the ViewModel when the selected tab changes
    }
}

public class ExplorerItem
{
    public enum ExplorerItemType
    {
        Folder,
        File,
    }

    public string? Name { get; set; }
    public ExplorerItemType Type { get; set; }
    public ObservableCollection<ExplorerItem> Children { get; set; } = new ObservableCollection<ExplorerItem>();
}

class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }
    protected override DataTemplate? SelectTemplateCore (object item)
    {
        var explorerItem = (ExplorerItem) item;
        return explorerItem.Type == ExplorerItem.ExplorerItemType.Folder
            ? FolderTemplate
            : FileTemplate;
    }
}