using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;
using System.Diagnostics;

namespace InkMD_Editor.ViewModels;

public partial class WordCountViewModel : ObservableObject
{
    [ObservableProperty]
    public partial int? WordCount { get; set; }

    [ObservableProperty]
    public partial string? FileName { get; set; }

    public WordCountViewModel ()
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
