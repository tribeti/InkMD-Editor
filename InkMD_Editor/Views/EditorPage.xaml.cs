using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Controls;
using InkMD_Editor.Messagers;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Views;

public sealed partial class EditorPage : Page
{
    public string rootPath = "D:\\Project\\aichatbot";
    public TabViewContentViewModel? ViewModel { get; } = new();
    public StoragePickerViewModel? MenuBarViewModel { get; } = new();

    public EditorPage ()
    {
        InitializeComponent();
        InitTreeView();
        WeakReferenceMessenger.Default.Register<WordCountMessage>(this , (r , msg) =>
            {
                WordCountText.Text = msg.WordCount?.ToString() ?? "0";
            }
        );
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

        if ( node is null )
            return;

        if ( node.Content is IStorageItem item )
        {
            if ( node.Content is StorageFolder )
            {
                node.IsExpanded = !node.IsExpanded;
                return;
            }

            if ( item is StorageFile file )
            {
                try
                {
                    var text = await ReadFileTextAsync(file);
                    var newTab = CreateNewTab(Tabs.TabItems.Count);
                    var content = (TabViewContent) newTab.Content!;
                    content.SetContent(text , file.Name);
                    newTab.Header = file.Name;

                    Tabs.TabItems.Add(newTab);
                    Tabs.SelectedItem = newTab;
                }
                catch ( Exception )
                {
                }
            }
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
        var _ = (TabViewContentViewModel) content.DataContext;
        newItem.Content = content;
        return newItem;
    }

    private async Task<string> ReadFileTextAsync (StorageFile file)
    {
        try
        {
            var buffer = await FileIO.ReadBufferAsync(file);
            byte [] bytes;
            using ( var dataReader = Windows.Storage.Streams.DataReader.FromBuffer(buffer) )
            {
                bytes = new byte [buffer.Length];
                dataReader.ReadBytes(bytes);
            }

            // BOM detection
            if ( bytes.Length >= 3 && bytes [0] == 0xEF && bytes [1] == 0xBB && bytes [2] == 0xBF )
            {
                // UTF-8 with BOM
                return Encoding.UTF8.GetString(bytes , 3 , bytes.Length - 3);
            }

            if ( bytes.Length >= 2 && bytes [0] == 0xFF && bytes [1] == 0xFE )
            {
                // UTF-16 LE
                return Encoding.Unicode.GetString(bytes , 2 , bytes.Length - 2);
            }

            if ( bytes.Length >= 2 && bytes [0] == 0xFE && bytes [1] == 0xFF )
            {
                // UTF-16 BE
                return Encoding.BigEndianUnicode.GetString(bytes , 2 , bytes.Length - 2);
            }

            // No BOM — try UTF-8 first, then UTF-16, then fall back to system/default encoding
            try
            {
                var s = Encoding.UTF8.GetString(bytes);
                return s;
            }
            catch { }

            try
            {
                var s = Encoding.Unicode.GetString(bytes);
                return s;
            }
            catch { }

            return Encoding.Default.GetString(bytes);
        }
        catch
        {
            return string.Empty;
        }
    }
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