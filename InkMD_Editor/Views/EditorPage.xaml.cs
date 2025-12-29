using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Interfaces;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Linq;
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
        WeakReferenceMessenger.Default.Register<FileOpenedMessage>(this , async (r , msg) => await OpenFile(msg.File));

        WeakReferenceMessenger.Default.Register<FolderOpenedMessage>(this , async (r , msg) => await RefreshTreeViewWithFolder(msg.Folder));

        WeakReferenceMessenger.Default.Register<SaveFileRequestMessage>(this , (r , msg) => SaveCurrentTabContent(msg.FilePath));

        WeakReferenceMessenger.Default.Register<SaveFileMessage>(this , async (r , msg) => await HandleSaveFile());

        WeakReferenceMessenger.Default.Register<ErrorMessage>(this , async (r , msg) => await _viewModel.ShowErrorAsync(msg.Message));

        WeakReferenceMessenger.Default.Register<TemplateSelectedMessage>(this , async (r , msg) => await HandleTemplateSelected(msg.Content , msg.CreateNewFile));

        WeakReferenceMessenger.Default.Register<ViewModeChangedMessage>(this , (r , msg) =>
        {
            var (_, content) = GetSelectedTabContent();
            if ( content is TabViewContent tabContent )
            {
                tabContent.SetViewMode(msg.NewMode);
            }
        });
    }

    private void UpdateMenuVisibility ()
    {
        var (_, tabContent) = GetSelectedTabContent();
        mainMenu.UpdateVisibilityForTab(tabContent);
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
        var currentFileName = tabContent.GetFileName();
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
            await OpenFile(file);
        }
    }

    private async Task OpenFile (StorageFile file)
    {
        try
        {
            if ( IsFileAlreadyOpen(file.Path) )
            {
                SelectExistingTab(file.Path);
                return;
            }

            var result = await _viewModel.OpenFileAsync(file);
            if ( result is null )
            {
                return;
            }

            var isMarkdown = _viewModel.IsMarkdownFile(file);

            var newTab = CreateNewTab(Tabs.TabItems.Count , isMarkdown);

            var content = (IEditableContent) newTab.Content!;
            content.SetFilePath(result.Value.filePath , result.Value.fileName);
            content.SetContent(result.Value.content , result.Value.fileName);

            newTab.Header = result.Value.fileName;
            Tabs.TabItems.Add(newTab);
            Tabs.SelectedItem = newTab;
            UpdateMenuVisibility();
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot open file: {ex.Message}");
        }
    }

    private bool IsFileAlreadyOpen (string filePath) => FindTabByFilePath(filePath) is not null;

    private void SelectExistingTab (string filePath)
    {
        var tab = FindTabByFilePath(filePath);
        if ( tab is not null )
        {
            Tabs.SelectedItem = tab;
        }
    }

    private TabViewItem? FindTabByFilePath (string filePath)
    {
        return Tabs.TabItems
            .OfType<TabViewItem>()
            .FirstOrDefault(tab => tab.Content is IEditableContent content && content.GetFilePath() == filePath);
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
        tab.Header = content.GetFileName();
    }

    private async void SaveCurrentTabContent (string filePath)
    {
        try
        {
            var (tab, content) = GetSelectedTabContent();
            if ( content is not null )
            {
                await _viewModel.SaveFileToPath(filePath , content);
                tab!.Header = content.GetFileName();
            }
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot save file: {ex.Message}");
        }
    }

    private void Button_Click (object sender , RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));

    private void TabView_SelectionChanged (object sender , SelectionChangedEventArgs e) => UpdateMenuVisibility();

    private void TabView_AddButtonClick (TabView sender , object args)
    {
        var newTab = CreateNewTab(sender.TabItems.Count , true);
        sender.TabItems.Add(newTab);
        sender.SelectedItem = newTab;
        UpdateMenuVisibility();
    }

    private void TabView_TabCloseRequested (TabView sender , TabViewTabCloseRequestedEventArgs args)
    {
        if ( args.Tab.Content is TabViewContent tabContent )
        {
            tabContent.DisposeWebView();
        }
        sender.TabItems.Remove(args.Tab);
        if ( sender.TabItems.Count == 0 )
        {
            mainMenu.SetVisibility(false);
        }
        else
        {
            UpdateMenuVisibility();
        }
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

    private async void DeleteItem_Click (object sender , RoutedEventArgs e)
    {
        if ( treeview.SelectedItem is not TreeViewNode node || node.Content is not IStorageItem item )
        {
            return;
        }
        bool isConfirmed = await _viewModel.ShowConfirmationAsync($"Do you want to delete: {item.Name}?");
        if ( isConfirmed )
        {
            if ( item is StorageFile file )
            {
                try
                {
                    var tabToClose = FindTabByFilePath(file.Path);
                    if ( tabToClose is not null )
                    {
                        Tabs.TabItems.Remove(tabToClose);
                    }
                    System.IO.File.Delete(file.Path);
                    node.Parent?.Children.Remove(node);
                }
                catch ( Exception ex )
                {
                    await _viewModel.ShowErrorAsync($"Error deleting file: {ex.Message}");
                }

            }
            else if ( item is StorageFolder folder )
            {
                try
                {
                    var tabsToRemove = Tabs.TabItems.OfType<TabViewItem>()
                    .Where(tab =>
                        tab.Content is IEditableContent content &&
                        !string.IsNullOrEmpty(content.GetFilePath()) &&
                        content.GetFilePath().StartsWith(folder.Path + System.IO.Path.DirectorySeparatorChar , StringComparison.OrdinalIgnoreCase))
                    .ToList();

                    foreach ( var tab in tabsToRemove )
                    {
                        Tabs.TabItems.Remove(tab);
                    }

                    System.IO.Directory.Delete(folder.Path , true);
                    node.Parent?.Children.Remove(node);
                }
                catch ( Exception ex )
                {
                    await _viewModel.ShowErrorAsync($"Error deleting folder: {ex.Message}");
                }
            }
        }
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