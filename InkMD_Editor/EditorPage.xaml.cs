using InkMD_Editor.Controls;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;

namespace InkMD_Editor;

public sealed partial class EditorPage : Page
{
    public ObservableCollection<ExplorerItem> DataSource { get; set; }
    public WordCountViewModel? ViewModel { get; } = new();
    public MenuBarViewModel? MenuBarViewModel { get; } = new();
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