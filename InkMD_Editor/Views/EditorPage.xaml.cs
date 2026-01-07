using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Interfaces;
using InkMD_Editor.Messages;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Views;

public sealed partial class EditorPage : Page
{
    private readonly EditorPageViewModel _viewModel;
    private readonly IDialogService _dialogService;

    public EditorPage ()
    {
        var app = (App) Application.Current;
        _viewModel = app.Services.GetRequiredService<EditorPageViewModel>();
        _dialogService = app.Services.GetRequiredService<IDialogService>();

        InitializeComponent();

        Loaded += async (s , e) =>
        {
            _dialogService.SetXamlRoot(XamlRoot);
            _viewModel.Initialize();
            await InitTreeViewAsync();
            SetupMessengers();
        };
    }

    private void SetupMessengers ()
    {
        var messenger = WeakReferenceMessenger.Default;

        messenger.Register<FileOpenedMessage>(this , async (r , msg) => await OpenFile(msg.File));
        messenger.Register<FolderOpenedMessage>(this , async (r , msg) => await RefreshTreeViewWithFolder(msg.Folder));
        messenger.Register<SaveFileRequestMessage>(this , (r , msg) => SaveCurrentTabContent(msg.FilePath));
        messenger.Register<SaveFileMessage>(this , async (r , msg) => await HandleSaveFile());
        messenger.Register<ErrorMessage>(this , async (r , msg) => await _viewModel.ShowErrorAsync(msg.Message));
        messenger.Register<TemplateSelectedMessage>(this , async (r , msg) => await HandleTemplateSelected(msg.Content , msg.CreateNewFile));
        messenger.Register<ContentChangedMessage>(this , (r , msg) => UpdateTabHeaderForDirtyState());

        messenger.Register<ViewModeChangedMessage>(this , (r , msg) =>
        {
            if ( GetSelectedTabContent().content is TabViewContent tabContent )
                tabContent.SetViewMode(msg.NewMode);
        });

        messenger.Register<EditCommandMessage>(this , (r , msg) => HandleEditCommand(msg.Command));
    }

    private void HandleEditCommand (EditCommandType command)
    {
        if ( GetSelectedTabContent().content is not IEditableContent editable )
            return;

        Action action = command switch
        {
            EditCommandType.Undo => editable.Undo,
            EditCommandType.Redo => editable.Redo,
            EditCommandType.Cut => editable.Cut,
            EditCommandType.Copy => editable.Copy,
            EditCommandType.Paste => editable.Paste,
            _ => () => { }
        };
        action();
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
            return;

        var newTab = CreateNewTab(Tabs.TabItems.Count , isMarkdown: true);
        if ( newTab.Content is TabViewContent tabContent )
        {
            tabContent.SetContent(result.content , $"Document {Tabs.TabItems.Count}");
        }

        AddAndSelectTab(newTab);
    }

    private async Task InsertIntoCurrentDocument (string content)
    {
        var (_, tabContent) = GetSelectedTabContent();
        if ( Tabs.TabItems.Count == 0 || tabContent is null )
        {
            await _viewModel.ShowErrorAsync("There is no open file. Please open or create one first.");
            return;
        }

        var (success, newContent, error) = await _viewModel.InsertIntoDocumentAsync(content , tabContent);

        if ( !success )
        {
            await _viewModel.ShowErrorAsync(error ?? "Unknown error");
            return;
        }

        tabContent.SetContent(newContent! , tabContent.GetFileName());
    }

    private async Task InitTreeViewAsync ()
    {
        if ( await _viewModel.InitializeTreeViewAsync() is { } node )
        {
            treeview.RootNodes.Add(node);
        }
    }

    private async Task RefreshTreeViewWithFolder (StorageFolder folder)
    {
        treeview.RootNodes.Clear();
        if ( await _viewModel.RefreshTreeViewWithFolderAsync(folder) is { } node )
        {
            treeview.RootNodes.Add(node);
        }
    }

    private void TreeView_Expanding (TreeView sender , TreeViewExpandingEventArgs args)
    {
        if ( args.Node.HasUnrealizedChildren )
            _ = _viewModel.FillTreeNodeAsync(args.Node);
    }

    private void TreeView_Collapsed (TreeView sender , TreeViewCollapsedEventArgs args) => _viewModel.CollapseTreeNode(args.Node);

    private async void TreeView_ItemInvoked (TreeView sender , TreeViewItemInvokedEventArgs args)
    {
        if ( args.InvokedItem is not TreeViewNode { Content: IStorageItem item } node )
            return;

        switch ( item )
        {
            case StorageFolder:
                node.IsExpanded = !node.IsExpanded;
                break;
            case StorageFile file:
                await OpenFile(file);
                break;
        }
    }

    private async Task OpenFile (StorageFile file)
    {
        try
        {
            if ( FindTabByFilePath(file.Path) is { } existingTab )
            {
                Tabs.SelectedItem = existingTab;
                return;
            }

            var result = await _viewModel.OpenFileAsync(file);
            if ( result is null )
                return;

            var (contentStr, fileName, filePath) = result.Value;
            var isMarkdown = _viewModel.IsMarkdownFile(file);
            var newTab = CreateNewTab(Tabs.TabItems.Count , isMarkdown);

            if ( newTab.Content is IEditableContent content )
            {
                content.SetFilePath(filePath , fileName);
                content.SetContent(contentStr , fileName);
            }

            newTab.Header = fileName;
            AddAndSelectTab(newTab);
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot open file: {ex.Message}");
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
        content.MarkAsClean();
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
                content.MarkAsClean();
                tab?.Header = content.GetFileName();
            }
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Cannot save file: {ex.Message}");
        }
    }

    private void AddAndSelectTab (TabViewItem newTab)
    {
        Tabs.TabItems.Add(newTab);
        Tabs.SelectedItem = newTab;
        UpdateMenuVisibility();
    }

    private TabViewItem CreateNewTab (int index , bool isMarkdown)
    {
        return new TabViewItem
        {
            IconSource = new SymbolIconSource { Symbol = Symbol.Document } ,
            Header = $"Document {index + 1}" ,
            Content = isMarkdown ? new TabViewContent() : new EditTabViewContent()
        };
    }

    private TabViewItem? FindTabByFilePath (string filePath) => Tabs.TabItems
            .OfType<TabViewItem>()
            .FirstOrDefault(tab => tab.Content is IEditableContent content && content.GetFilePath() == filePath);

    private (TabViewItem? tab, IEditableContent? content) GetSelectedTabContent () =>
        Tabs.SelectedItem is TabViewItem tab ? (tab, tab.Content as IEditableContent) : (null, null);

    private void UpdateTabHeaderForDirtyState ()
    {
        var (tab, content) = GetSelectedTabContent();
        if ( tab is not null && content is not null )
        {
            tab.Header = content.IsDirty() ? $"{content.GetFileName()} ●" : content.GetFileName();
        }
    }

    private void Button_Click (object sender , RoutedEventArgs e) => Frame.Navigate(typeof(SettingsPage));
    private void TabView_SelectionChanged (object sender , SelectionChangedEventArgs e) => UpdateMenuVisibility();
    private void TabView_AddButtonClick (TabView sender , object args) => AddAndSelectTab(CreateNewTab(sender.TabItems.Count , true));

    private async void TabView_TabCloseRequested (TabView sender , TabViewTabCloseRequestedEventArgs args)
    {
        if ( args.Tab.Content is IEditableContent { } content && content.IsDirty() )
        {
            if ( !await _viewModel.ShowConfirmationAsync($"Do you want to close '{content.GetFileName()}' without saving changes?") )
                return;
        }

        if ( args.Tab.Content is TabViewContent tabContent )
            tabContent.DisposeWebView();

        sender.TabItems.Remove(args.Tab);

        if ( sender.TabItems.Count == 0 )
            mainMenu.SetVisibility(false);
        else
            UpdateMenuVisibility();
    }

    private async void DeleteItem_Click (object sender , RoutedEventArgs e)
    {
        if ( treeview.SelectedItem is not TreeViewNode { Content: IStorageItem item } node )
            return;

        if ( !await _viewModel.ShowConfirmationAsync($"Do you want to delete: {item.Name}?") )
            return;

        try
        {
            if ( item is StorageFile file )
            {
                var tabToClose = FindTabByFilePath(file.Path);
                await file.DeleteAsync();

                if ( tabToClose is not null )
                {
                    if ( tabToClose.Content is TabViewContent t )
                        t.DisposeWebView();
                    Tabs.TabItems.Remove(tabToClose);
                }
            }
            else if ( item is StorageFolder folder )
            {
                var tabsToRemove = Tabs.TabItems.OfType<TabViewItem>()
                    .Where(tab => tab.Content is IEditableContent content && IsDescendantPath(content.GetFilePath() , folder.Path))
                    .ToList();

                await folder.DeleteAsync();

                foreach ( var tab in tabsToRemove )
                {
                    if ( tab.Content is TabViewContent t )
                        t.DisposeWebView();
                    Tabs.TabItems.Remove(tab);
                }
            }

            if ( node.Parent is { } parent )
                parent.Children.Remove(node);
            else
                treeview.RootNodes.Remove(node);
        }
        catch ( Exception ex )
        {
            await _viewModel.ShowErrorAsync($"Error deleting item: {ex.Message}");
        }
    }

    private static bool IsDescendantPath (string descendantPath , string ancestorPath)
    {
        if ( string.IsNullOrEmpty(descendantPath) || string.IsNullOrEmpty(ancestorPath) )
            return false;

        try
        {
            var normalizedDescendant = Path.GetFullPath(descendantPath).TrimEnd(Path.DirectorySeparatorChar , Path.AltDirectorySeparatorChar);
            var normalizedAncestor = Path.GetFullPath(ancestorPath).TrimEnd(Path.DirectorySeparatorChar , Path.AltDirectorySeparatorChar);

            return normalizedDescendant.StartsWith(normalizedAncestor + Path.DirectorySeparatorChar , StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}

public sealed partial class ExplorerItemTemplateSelector : DataTemplateSelector
{
    public DataTemplate? FolderTemplate { get; set; }
    public DataTemplate? FileTemplate { get; set; }

    protected override DataTemplate? SelectTemplateCore (object item) => item switch
    {
        TreeViewNode { Content: StorageFolder } => FolderTemplate,
        StorageFolder => FolderTemplate,
        _ => FileTemplate
    };
}