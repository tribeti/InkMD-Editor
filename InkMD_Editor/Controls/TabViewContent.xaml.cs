using InkMD_Editor.Helpers;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;

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

    public void SetContent (string text , string? fileName)
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
                return string.Empty;
            }

            var doc = MdEditor.Document;
            if ( doc is null )
            {
                return string.Empty;
            }

            doc.GetText(TextGetOptions.None , out string text);
            return text ?? string.Empty;
        }
        catch ( Exception )
        {
            return string.Empty;
        }
    }

    private async void InitializeWebView ()
    {
        try
        {
            await MarkdownPreview.EnsureCoreWebView2Async();
            MarkdownPreview.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
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
            return GitHubPreview.GetEmptyPreviewHtml();

        string htmlBody = Markdown.ToHtml(markdown , _markdownPipeline);

        return GitHubPreview.WrapWithGitHubStyle(htmlBody);
    }

    public void DisposeWebView ()
    {
        MarkdownPreview?.Close();
    }
}