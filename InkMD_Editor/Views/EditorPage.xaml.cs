using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Interfaces;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
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
        InitTreeView();
        SetupMessengers();
        Loaded += EditorPage_Loaded;
    }

    private void EditorPage_Loaded (object sender , RoutedEventArgs e)
    {
        _dialogService.SetXamlRoot(XamlRoot);
        _viewModel.SetDialogService(_dialogService);
    }

    private void SetupMessengers ()
    {
        WeakReferenceMessenger.Default.Register<FileOpenedMessage>(this , (r , msg) => OpenFileInNewTab(msg.File));

        WeakReferenceMessenger.Default.Register<FolderOpenedMessage>(this , async (r , msg) => await RefreshTreeViewWithFolder(msg.Folder));

        WeakReferenceMessenger.Default.Register<SaveFileRequestMessage>(this , (r , msg) => SaveCurrentTabContent(msg.FilePath));

        WeakReferenceMessenger.Default.Register<SaveFileMessage>(this , async (r , msg) => await HandleSaveFile());

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this , async (r , msg) => await _viewModel.ShowErrorAsync(msg.Message));

        WeakReferenceMessenger.Default.Register<TemplateSelectedMessage>(this , async (r , msg) => await HandleTemplateSelected(msg.Content , msg.CreateNewFile));
    }

    private async Task HandleTemplateSelected (string content , bool createNewFile)
    {
        if ( createNewFile )
        {
            CreateNewTabWithContent(content);
        }
        else
        {
            await InsertIntoCurrentDocument(content);
        }
    }

    private void CreateNewTabWithContent (string content)
    {
        var result = _viewModel.CreateNewTabContent(content , Tabs.TabItems.Count);
        if ( !result.success || result.content is null )
        {
            return;
        }

        var newTab = CreateNewTab(Tabs.TabItems.Count , true);
        var tabContent = (TabViewContent) newTab.Content!;
        tabContent.SetContent(result.content , $"Document {Tabs.TabItems.Count}");

        Tabs.TabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
    }

    private async Task InsertIntoCurrentDocument (string content)
    {
        if ( Tabs.TabItems.Count == 0 || Tabs.SelectedItem is null )
        {
            await _viewModel.ShowErrorAsync("There is no open file. Please open or create one first.");
            return;
        }

        var (_, tabContent) = GetSelectedTabContent();
        var (success, newContent, error) = await _viewModel.InsertIntoDocumentAsync(content , tabContent);

        if ( !success )
        {
            await _viewModel.ShowErrorAsync(error ?? "Unknown error");
            return;
        }

        string? currentFileName = string.Empty;
        if ( tabContent is TabViewContent tvc )
            currentFileName = tvc.ViewModel.FileName;
        else if ( tabContent is EditTabViewContent evc )
            currentFileName = evc.ViewModel.FileName;

        tabContent.SetContent(newContent! , currentFileName);
    }

    private async void InitTreeView ()
    {
        var node = await _viewModel.InitializeTreeViewAsync();
        if ( node is not null )
        {
            treeview.RootNodes.Add(node);
        }
    }

    private async Task RefreshTreeViewWithFolder (StorageFolder folder)
    {
        treeview.RootNodes.Clear();
        var node = await _viewModel.RefreshTreeViewWithFolderAsync(folder);
        if ( node is not null )
        {
            treeview.RootNodes.Add(node);
        }
    }

    private void TreeView_Expanding (TreeView sender , TreeViewExpandingEventArgs args)
    {
        if ( args.Node.HasUnrealizedChildren )
        {
            _ = _viewModel.FillTreeNodeAsync(args.Node);
        }
    }

    private void TreeView_Collapsed (TreeView sender , TreeViewCollapsedEventArgs args)
    {
        _viewModel.CollapseTreeNode(args.Node);
    }

    private async void TreeView_ItemInvoked (TreeView sender , TreeViewItemInvokedEventArgs args)
    {
        if ( args.InvokedItem is not TreeViewNode node || node.Content is not IStorageItem item )
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
            await OpenFileFromTreeView(file);
        }
    }

    private async Task OpenFileFromTreeView (StorageFile file)
    {
        var result = await _viewModel.OpenFileAsync(file);
        if ( result is null )
        {
            return;
        }

        var isMarkdown = _viewModel.IsMarkdownFile(file);
        mainMenu.SetVisibility(isMarkdown);

        var newTab = CreateNewTab(Tabs.TabItems.Count , isMarkdown);

        if ( isMarkdown )
        {
            var content = (TabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(result.Value.filePath , result.Value.fileName);
            content.SetContent(result.Value.content , result.Value.fileName);
        }
        else
        {
            var content = (EditTabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(result.Value.filePath , result.Value.fileName);
            content.SetContent(result.Value.content , result.Value.fileName);
        }

        newTab.Header = result.Value.fileName;
        Tabs.TabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
    }

    private async void OpenFileInNewTab (StorageFile file)
    {
        var result = await _viewModel.OpenFileAsync(file);
        if ( result is null )
        {
            return;
        }

        var isMarkdown = _viewModel.IsMarkdownFile(file);
        mainMenu.SetVisibility(isMarkdown);

        var newTab = CreateNewTab(Tabs.TabItems.Count , isMarkdown);

        if ( isMarkdown )
        {
            var content = (TabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(result.Value.filePath , result.Value.fileName);
            content.SetContent(result.Value.content , result.Value.fileName);
        }
        else
        {
            var content = (EditTabViewContent) newTab.Content!;
            content.ViewModel.SetFilePath(result.Value.filePath , result.Value.fileName);
            content.SetContent(result.Value.content , result.Value.fileName);
        }

        newTab.Header = result.Value.fileName;
        Tabs.TabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
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
        if ( content is TabViewContent tvc )
            tab.Header = tvc.ViewModel.FileName;
        else if ( content is EditTabViewContent evc )
            tab.Header = evc.ViewModel.FileName;
    }

    private async void SaveCurrentTabContent (string filePath)
    {
        try
        {
            var (tab, content) = GetSelectedTabContent();
            if ( content is not null )
            {
                await _viewModel.SaveFileToPath(filePath , content);
                if ( content is TabViewContent tvc )
                    tab.Header = tvc.ViewModel.FileName;
                else if ( content is EditTabViewContent evc )
                    tab.Header = evc.ViewModel.FileName;
            }
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot save file: {ex.Message}");
        }
    }

    private void Button_Click (object sender , RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));

    private void TabView_AddButtonClick (TabView sender , object args)
    {
        var newTab = CreateNewTab(sender.TabItems.Count);
        sender.TabItems.Add(newTab);
    }

    private void TabView_TabCloseRequested (TabView sender , TabViewTabCloseRequestedEventArgs args)
    {
        if ( args.Tab.Content is TabViewContent tabContent )
        {
            tabContent.DisposeWebView();
        }
        sender.TabItems.Remove(args.Tab);
    }

    private static TabViewItem CreateNewTab (int index)
    {
        var newItem = new TabViewItem
        {
            Header = $"Document {index}" ,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document }
        };

        var content = new TabViewContent();
        if ( content.DataContext is TabViewContentViewModel viewModel )
        {
            viewModel.ResetForNewFile();
        }

        newItem.Content = content;
        return newItem;
    }

    private TabViewItem CreateNewTab (int index , bool isMarkdown)
    {
        var newTab = new TabViewItem
        {
            IconSource = new SymbolIconSource { Symbol = Symbol.Document } ,
            Header = $"Document {index + 1}"
        };

        if ( isMarkdown )
        {
            newTab.Content = new TabViewContent();
        }
        else
        {
            newTab.Content = new EditTabViewContent();
        }

        return newTab;
    }

    private (TabViewItem? tab, IEditableContent? content) GetSelectedTabContent () =>
        Tabs.SelectedItem is TabViewItem tab ? (tab, tab.Content as IEditableContent) : (null, null);
}

public sealed partial class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore (object item) =>
        item switch
        {
            TreeViewNode { Content: StorageFolder } => FolderTemplate,
            TreeViewNode => FileTemplate,
            StorageFolder => FolderTemplate,
            _ => FileTemplate
        };
}