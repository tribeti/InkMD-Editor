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

        InitializeWebViews();
        InitializeEditBoxes();

        SetViewMode("split");
        this.Loaded += TabViewContent_Loaded;
    }

    public void SetViewMode (string tag)
    {
        string currentText = GetCurrentEditBoxText();
        ViewModel.Tag = tag;

        if ( !string.IsNullOrEmpty(currentText) )
        {
            SetContentToCurrentEditBox(currentText);
        }
    }

    private void TabViewContent_Loaded (object sender , Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if ( !string.IsNullOrEmpty(ViewModel.CurrentContent) )
        {
            SetContentToCurrentEditBox(ViewModel.CurrentContent);
            UpdateMarkdownPreview(ViewModel.CurrentContent);
        }
    }

    private void InitializeEditBoxes ()
    {
        if ( EditBox is not null )
        {
            EditBox.EnableSyntaxHighlighting = true;
            EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
        }

        if ( EditBox_Split is not null )
        {
            EditBox_Split.EnableSyntaxHighlighting = true;
            EditBox_Split.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
        }
    }

    private void EditBox_TextChanged (TextControlBox sender)
    {
        string text = GetCurrentEditBoxText();
        UpdateMarkdownPreview(text);
        ViewModel.CurrentContent = text;
    }

    private string GetCurrentEditBoxText ()
    {
        return ViewModel.Tag switch
        {
            "md" => EditBox?.GetText() ?? string.Empty,
            "split" => EditBox_Split?.GetText() ?? string.Empty,
            _ => string.Empty
        };
    }

    public void SetContent (string text , string? fileName)
    {
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
        SetContentToCurrentEditBox(text);
        UpdateMarkdownPreview(text);
    }

    private void SetContentToCurrentEditBox (string text)
    {
        switch ( ViewModel.Tag )
        {
            case "md":
                EditBox?.SetText(text);
                break;
            case "split":
                EditBox_Split?.SetText(text);
                break;
        }
    }

    public string GetContent ()
    {
        return GetCurrentEditBoxText();
    }

    public string GetFilePath () => ViewModel.FilePath ?? string.Empty;

    public string GetFileName () => ViewModel.FileName ?? string.Empty;

    public void SetFilePath (string filePath , string fileName) => ViewModel.SetFilePath(filePath , fileName);

    private async void InitializeWebViews ()
    {
        try
        {
            if ( MarkdownPreview_Split is not null )
            {
                await MarkdownPreview_Split.EnsureCoreWebView2Async();
                MarkdownPreview_Split.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
            }

            if ( MarkdownPreview is not null )
            {
                await MarkdownPreview.EnsureCoreWebView2Async();
                MarkdownPreview.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
            }
        }
        catch { }
    }

    private void UpdateMarkdownPreview (string markdownText)
    {
        try
        {
            string html = ConvertMarkdownToHtml(markdownText);
            switch ( ViewModel.Tag )
            {
                case "split":
                    if ( MarkdownPreview_Split?.CoreWebView2 is not null )
                    {
                        MarkdownPreview_Split.NavigateToString(html);
                    }
                    break;
                case "preview":
                    if ( MarkdownPreview?.CoreWebView2 is not null )
                    {
                        MarkdownPreview.NavigateToString(html);
                    }
                    break;
            }
        }
        catch { }
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
        MarkdownPreview?.Close();
    }
}