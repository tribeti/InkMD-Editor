using InkMD_Editor.Controls;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using Windows.Storage;
namespace InkMD_Editor;

public sealed partial class EditorPage : Page
{
    public string rootPath = "D:\\Project\\aichatbot";
    public WordCountViewModel? ViewModel { get; } = new();
    public MenuBarViewModel? MenuBarViewModel { get; } = new();

    public EditorPage ()
    {
        InitializeComponent();
        InitTreeView();
    }

    public async void InitTreeView ()
    {
        try
        {
            StorageFolder folder = await StorageFolder.GetFolderFromPathAsync(rootPath);
            TreeViewNode node = new TreeViewNode();
            node.Content = folder;
            node.IsExpanded = true;
            node.HasUnrealizedChildren = true;
            treeview.RootNodes.Add(node);
            FillTreeNode(node);

        }
        catch ( Exception ) { }
    }

    private async void FillTreeNode (TreeViewNode node)
    {
        StorageFolder? folder = null;
        if ( node.Content is StorageFolder && node.HasUnrealizedChildren == true )
        {
            folder = node.Content as StorageFolder;
        }
        else
        {
            return;
        }

        if ( folder is null )
        {
            return;
        }

        IReadOnlyList<IStorageItem> itemsList = await folder.GetItemsAsync();

        if ( itemsList.Count == 0 )
        {
            return;
        }

        foreach ( var item in itemsList )
        {
            var newNode = new TreeViewNode();
            newNode.Content = item;

            if ( item is StorageFolder )
            {
                newNode.HasUnrealizedChildren = true;
            }
            node.Children.Add(newNode);
        }
        node.HasUnrealizedChildren = false;
    }


    private void SampleTreeView_Expanding (TreeView sender , TreeViewExpandingEventArgs args)
    {
        if ( args.Node.HasUnrealizedChildren )
        {
            FillTreeNode(args.Node);
        }
    }

    private void SampleTreeView_Collapsed (TreeView sender , TreeViewCollapsedEventArgs args)
    {
        args.Node.Children.Clear();
        args.Node.HasUnrealizedChildren = true;
    }

    private void SampleTreeView_ItemInvoked (TreeView sender , TreeViewItemInvokedEventArgs args)
    {
        var node = args.InvokedItem as TreeViewNode;
        if ( node?.Content is StorageFolder folder )
        {
            node.IsExpanded = !node.IsExpanded;
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
        TabViewItem newItem = new TabViewItem
        {
            Header = $"Document {index}" ,
            IconSource = new SymbolIconSource { Symbol = Symbol.Document }
        };

        var content = new TabViewContent();
        var viewModel = (WordCountViewModel) content.DataContext;
        newItem.Content = content;
        return newItem;
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
    public string? FullPath { get; set; }
    public bool HasUnloadedChildren { get; set; }
}

class ExplorerItemTemplateSelector : DataTemplateSelector
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