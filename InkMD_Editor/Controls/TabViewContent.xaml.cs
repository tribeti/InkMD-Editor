using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl
{
    public TabViewContentViewModel ViewModel { get; set; } = new();
    private readonly MarkdownPipeline _markdownPipeline;

    public TabViewContent ()
    {
        InitializeComponent();
        this.DataContext = ViewModel;

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();

        InitializeWebView();
    }

    private void MdEditor_TextChanged (object sender , RoutedEventArgs e)
    {
        var doc = MdEditor.Document;
        doc.GetText(TextGetOptions.None , out string text);
        UpdateMarkdownPreview(text);
    }

    public void SetContent (string text , string fileName)
    {
        var doc = MdEditor.Document;
        doc.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
        UpdateMarkdownPreview(text);
    }

    public string GetContent ()
    {
        try
        {
            if ( MdEditor is null )
            {
                Debug.WriteLine("MdEditor is null");
                return string.Empty;
            }

            var doc = MdEditor.Document;
            if ( doc is null )
            {
                Debug.WriteLine("Document is null");
                return string.Empty;
            }

            doc.GetText(TextGetOptions.None , out string text);
            Debug.WriteLine($"GetContent: Retrieved {text?.Length ?? 0} characters");
            return text ?? string.Empty;
        }
        catch ( Exception ex )
        {
            Debug.WriteLine($"GetContent error: {ex.Message}");
            return string.Empty;
        }
    }

    private async void InitializeWebView ()
    {
        try
        {
            await MarkdownPreview.EnsureCoreWebView2Async();
            MarkdownPreview.NavigateToString(GetEmptyPreviewHtml());
        }
        catch
        {
        }
    }

    private void UpdateMarkdownPreview (string markdownText)
    {
        if ( MarkdownPreview?.CoreWebView2 is null )
            return;

        try
        {
            string html = ConvertMarkdownToHtml(markdownText);
            MarkdownPreview.NavigateToString(html);
        }
        catch
        {
        }
    }

    private string ConvertMarkdownToHtml (string markdown)
    {
        if ( string.IsNullOrWhiteSpace(markdown) )
            return GetEmptyPreviewHtml();

        string htmlBody = Markdown.ToHtml(markdown , _markdownPipeline);

        return WrapWithGitHubStyle(htmlBody);
    }

    private static string GetEmptyPreviewHtml ()
    {
        return WrapWithGitHubStyle("<p style='color:#888; text-align:center; margin-top:50px;'>Preview sẽ hiển thị ở đây...</p>");
    }

    public void DisposeWebView ()
    {
        MarkdownPreview?.Close();
    }

    private static string WrapWithGitHubStyle (string htmlBody)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/gh/tribeti/Java@master/style.css"">
</head>
<body>
    {htmlBody}
</body>
</html>";
    }
}
