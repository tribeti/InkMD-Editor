using InkMD_Editor.Helpers;
using InkMD_Editor.Interfaces;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Xaml.Controls;
using System;
using TextControlBoxNS;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl, IEditableContent
{
    public TabViewContentViewModel ViewModel { get; } = new();
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

        EditBox_Split.EnableSyntaxHighlighting = true;
        EditBox_Split.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
    }

    private void EditBox_TextChanged (TextControlBox sender)
    {
        string text = EditBox_Split.GetText();
        UpdateMarkdownPreview(text);
        ViewModel.CurrentContent = text;
    }

    public void SetContent (string text , string? fileName)
    {
        EditBox_Split.SetText(text);
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
        UpdateMarkdownPreview(text);
    }

    public string GetContent () => EditBox_Split.GetText() ?? string.Empty;

    public string GetFilePath () => ViewModel.FilePath ?? string.Empty;

    public string GetFileName () => ViewModel.FileName ?? string.Empty;

    public void SetFilePath (string filePath , string fileName) => ViewModel.SetFilePath(filePath , fileName);

    private async void InitializeWebView ()
    {
        try
        {
            await MarkdownPreview_Split.EnsureCoreWebView2Async();
            MarkdownPreview_Split.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
        }
        catch
        {

        }
    }

    private void UpdateMarkdownPreview (string markdownText)
    {
        if ( MarkdownPreview_Split?.CoreWebView2 is null )
            return;

        try
        {
            string html = ConvertMarkdownToHtml(markdownText);
            MarkdownPreview_Split.NavigateToString(html);
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
        MarkdownPreview_Split?.Close();
    }
}