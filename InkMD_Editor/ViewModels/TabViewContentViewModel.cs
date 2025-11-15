using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;

namespace InkMD_Editor.ViewModels;

public partial class TabViewContentViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int? WordCount { get; set; }

    [ObservableProperty]
    public partial string? FileName { get; set; }

    public TabViewContentViewModel ()
    {
        WordCount = 0;
        FileName = "Untitled";
    }

    public void UpdateWordCount (string text)
    {
        if ( string.IsNullOrWhiteSpace(text) )
        {
            WordCount = 0;
            return;
        }
        WordCount = System.Text.RegularExpressions.Regex.Matches(text , @"\b\w+\b").Count;
        WeakReferenceMessenger.Default.Send(
            new WordCountMessage { WordCount = this.WordCount }
        );
    }
}
