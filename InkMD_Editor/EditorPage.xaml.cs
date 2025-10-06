using InkMD_Editor.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor;

public sealed partial class EditorPage : Page
{
    public EditorPage ()
    {
        InitializeComponent();
        Loaded += EditorPage_Loaded;
    }

    private void Button_Click (object sender , RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void EditorPage_Loaded (object sender , RoutedEventArgs e)
    {
        for ( int i = 0 ; i < 3 ; i++ )
        {
            Tabs.TabItems.Add(CreateNewTab(i));
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

        var content = new TabViewContent
        {
            DataContext = $"Document {index}"
        };

        newItem.Content = content;

        return newItem;
    }
}