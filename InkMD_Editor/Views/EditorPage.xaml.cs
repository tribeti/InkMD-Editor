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

        WeakReferenceMessenger.Default.Register<FolderOpenedMessage>(this , async (r , msg) =>
        {
            await RefreshTreeViewWithFolder(msg.Folder);
        });

        WeakReferenceMessenger.Default.Register<SaveFileRequestMessage>(this , (r , msg) =>
        {
            SaveCurrentTabContent(msg.FilePath);
        });

        WeakReferenceMessenger.Default.Register<SaveFileMessage>(this , async (r , msg) =>
        {
            await HandleSaveFile();
        });

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this , async (r , msg) =>
        {
            await _viewModel.ShowErrorAsync(msg.Message);
        });

        WeakReferenceMessenger.Default.Register<TemplateSelectedMessage>(this , (r , msg) =>
        {
            HandleTemplateSelected(msg.Content , msg.CreateNewFile);
        });
    }

    private void HandleTemplateSelected (string content , bool createNewFile)
    {
        if ( createNewFile )
        {
            // Create new tab with template content
            CreateNewTabWithContent(content);
        }
        else
        {
            // Insert into current document
            InsertIntoCurrentDocument(content);
        }
    }

    private void CreateNewTabWithContent (string content)
    {
        var newTab = CreateNewTab(Tabs.TabItems.Count);
        var tabContent = (TabViewContent) newTab.Content!;
        tabContent.SetContent(content , $"Document {Tabs.TabItems.Count}");

        Tabs.TabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
    }

    private async void InsertIntoCurrentDocument (string content)
    {
        var (tab, tabContent) = GetSelectedTabContent();

        if ( tabContent is null )
        {
            // No document open, create a new one
            CreateNewTabWithContent(content);
            return;
        }

        try
        {
            // Get current content
            string currentContent = tabContent.GetContent();

            // Append template content
            string newContent = string.IsNullOrWhiteSpace(currentContent)
                ? content
                : currentContent + "\n\n" + content;

            // Set the combined content
            tabContent.SetContent(newContent , tabContent.ViewModel.FileName);
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot insert template: {ex.Message}");
        }
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
            await _viewModel.ShowErrorAsync($"TreeView Init Error: {ex.Message}");
        }
    }

    private async Task RefreshTreeViewWithFolder (StorageFolder folder)
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
            await _viewModel.ShowErrorAsync($"TreeView can not refresh: {ex.Message}");
        }
    }

    private async Task HandleSaveFile ()
    {
        var (tab, content) = GetSelectedTabContent();
        if ( tab is null || content is null )
        {
            await _viewModel.ShowErrorAsync("There is no open document");
            return;
        }

        await _viewModel.HandleSaveFile(content);
        tab.Header = content.ViewModel.FileName;
    }

    private async void SaveCurrentTabContent (string filePath)
    {
        try
        {
            var (tab, content) = GetSelectedTabContent();
            if ( content is not null )
            {
                await _viewModel.SaveFileToPath(filePath , content);
                tab.Header = content.ViewModel.FileName;
            }
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Can not save file: {ex.Message}");
        }
    }

    private async void OpenFileInNewTab (StorageFile file)
    {
        try
        {
            var text = await EditorPageViewModel.ReadFileTextAsync(file);
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
            await _viewModel.ShowErrorAsync($"Can not open file: {ex.Message}");
        }
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
            await _viewModel.ShowErrorAsync($"Can not load items: {ex.Message}");
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

            var text = await EditorPageViewModel.ReadFileTextAsync(file);
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
            await _viewModel.ShowErrorAsync($"Can not open file: {ex.Message}");
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

    private static TabViewItem CreateNewTab (int index)
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
    private static TreeViewNode CreateTreeViewNode (StorageFolder folder)
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
        if ( Tabs.SelectedItem is not TabViewItem tab )
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