using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Views;

public sealed partial class EditorPage : Page
{
    private readonly FileService _fileService = new();
    private readonly DialogService _dialogService = new();
    private readonly EditorPageViewModel _viewModel = new();

    public EditorPage ()
    {
        InitializeComponent();
        _dialogService.SetXamlRoot(this.XamlRoot);
        _viewModel.SetDialogService(_dialogService);
        InitTreeView();
        SetupMessengers();
    }

    private void SetupMessengers ()
    {
        WeakReferenceMessenger.Default.Register<FileOpenedMessage>(this , (r , msg) =>
        {
            OpenFileInNewTab(msg.File);
        });

        WeakReferenceMessenger.Default.Register<FolderOpenedMessage>(this , (r , msg) =>
        {
            RefreshTreeViewWithFolder(msg.Folder);
        });

        WeakReferenceMessenger.Default.Register<SaveFileRequestMessage>(this , (r , msg) =>
        {
            SaveCurrentTabContent(msg.FilePath);
        });

        WeakReferenceMessenger.Default.Register<SaveFileMessage>(this , async (r , msg) =>
        {
            await HandleSaveFile();
        });

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this , (r , msg) =>
        {
            ShowErrorDialog(msg.Message);
        });
    }

    public async void InitTreeView ()
    {
        try
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(_viewModel.RootPath);
            TreeViewNode node = CreateTreeViewNode(folder);
            treeview.RootNodes.Add(node);
            FillTreeNode(node);
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Lỗi khởi tạo TreeView: {ex.Message}");
        }
    }

    private void RefreshTreeViewWithFolder (StorageFolder folder)
    {
        try
        {
            treeview.RootNodes.Clear();
            TreeViewNode node = CreateTreeViewNode(folder);
            treeview.RootNodes.Add(node);
            FillTreeNode(node);
            _viewModel.RootPath = folder.Path;
        }
        catch ( Exception ex )
        {
            _ = _viewModel.ShowErrorAsync($"Lỗi làm mới TreeView: {ex.Message}");
        }
    }

    private async Task HandleSaveFile ()
    {
        var selectedTab = GetSelectedTabContent();
        if ( selectedTab.tab is null || selectedTab.content is null )
        {
            await _viewModel.ShowErrorAsync("Không có tab nào được chọn");
            return;
        }

        await _viewModel.HandleSaveFile(selectedTab.content);
        selectedTab.tab.Header = selectedTab.content.ViewModel.FileName;
    }

    private async void SaveCurrentTabContent (string filePath)
    {
        try
        {
            var selectedTab = GetSelectedTabContent();
            if ( selectedTab.content is not null )
            {
                await _viewModel.SaveFileToPath(filePath , selectedTab.content);
                selectedTab.tab.Header = selectedTab.content.ViewModel.FileName;
            }
        }
        catch ( Exception ex )
        {
            ShowErrorDialog($"Lỗi lưu file: {ex.Message}");
        }
    }

    private async void OpenFileInNewTab (StorageFile file)
    {
        try
        {
            var text = await _viewModel.ReadFileTextAsync(file);
            var newTab = CreateNewTab(Tabs.TabItems.Count);
            var content = (TabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(file.Path , file.Name);
            content.SetContent(text , file.Name);
            newTab.Header = file.Name;

            Tabs.TabItems.Add(newTab);
            Tabs.SelectedItem = newTab;
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Lỗi mở file: {ex.Message}");
        }
    }

    private async void ShowErrorDialog (string message)
    {
        await _viewModel.ShowErrorAsync(message);
    }

    private async void FillTreeNode (TreeViewNode node)
    {
        StorageFolder? folder = GetStorageFolder(node);
        if ( folder is null )
        {
            return;
        }

        try
        {
            IReadOnlyList<IStorageItem> itemsList = await folder.GetItemsAsync();

            if ( itemsList.Count == 0 )
            {
                node.HasUnrealizedChildren = false;
                return;
            }

            foreach ( var item in itemsList )
            {
                var newNode = new TreeViewNode { Content = item };
                if ( item is StorageFolder )
                {
                    newNode.HasUnrealizedChildren = true;
                }
                node.Children.Add(newNode);
            }
            node.HasUnrealizedChildren = false;
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Lỗi tải items: {ex.Message}");
        }
    }

    private void TreeView_Expanding (TreeView sender , TreeViewExpandingEventArgs args)
    {
        if ( args.Node.HasUnrealizedChildren )
        {
            FillTreeNode(args.Node);
        }
    }

    private void TreeView_Collapsed (TreeView sender , TreeViewCollapsedEventArgs args)
    {
        args.Node.Children.Clear();
        args.Node.HasUnrealizedChildren = true;
    }

    private async void TreeView_ItemInvoked (TreeView sender , TreeViewItemInvokedEventArgs args)
    {
        var node = args.InvokedItem as TreeViewNode;
        if ( node?.Content is not IStorageItem item )
        {
            return;
        }

        if ( item is StorageFolder )
        {
            node.IsExpanded = !node.IsExpanded;
            return;
        }

        if ( item is StorageFile file )
        {
            await OpenFileFromTreeView(file , node);
        }
    }

    private async Task OpenFileFromTreeView (StorageFile file , TreeViewNode node)
    {
        try
        {
            bool isMarkdownFile = file.FileType.Equals(".md" , StringComparison.OrdinalIgnoreCase);
            mainMenu.SetVisibility(isMarkdownFile);

            var text = await _viewModel.ReadFileTextAsync(file);
            var newTab = CreateNewTab(Tabs.TabItems.Count);
            var content = (TabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(file.Path , file.Name);
            content.SetContent(text , file.Name);
            newTab.Header = file.Name;

            Tabs.TabItems.Add(newTab);
            Tabs.SelectedItem = newTab;
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Lỗi mở file: {ex.Message}");
        }
    }

    private void Button_Click (object sender , RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
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
        TabViewItem newItem = new()
        {
            Header = $"Document {index}" ,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document }
        };

        var content = new TabViewContent();
        var viewModel = (TabViewContentViewModel) content.DataContext;
        viewModel.ResetForNewFile();
        newItem.Content = content;
        return newItem;
    }

    // Helper Methods
    private TreeViewNode CreateTreeViewNode (StorageFolder folder)
    {
        return new()
        {
            Content = folder ,
            IsExpanded = true ,
            HasUnrealizedChildren = true
        };
    }

    private (TabViewItem? tab, TabViewContent? content) GetSelectedTabContent ()
    {
        var tab = Tabs.SelectedItem as TabViewItem;
        if ( tab is null )
        {
            return (null, null);
        }

        var content = tab.Content as TabViewContent;
        return (tab, content);
    }

    private static StorageFolder? GetStorageFolder (TreeViewNode node)
    {
        if ( node.Content is StorageFolder && node.HasUnrealizedChildren )
        {
            return node.Content as StorageFolder;
        }
        return null;
    }
}

partial class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore (object item)
    {
        if ( item is TreeViewNode node )
        {
            return node.Content is StorageFolder ? FolderTemplate : FileTemplate;
        }

        return item is StorageFolder ? FolderTemplate : FileTemplate;
    }
}