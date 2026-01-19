using InkMD_Editor.Helpers;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using TextControlBoxNS;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl, IEditableContent
{
    public TabViewContentViewModel ViewModel { get; } = new();
    private readonly MarkdownPipeline _markdownPipeline;

    public TabViewContent()
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

    public void SetViewMode(string tag)
    {
        string currentText = GetCurrentEditBoxText();
        if (!string.IsNullOrEmpty(currentText))
        {
            ViewModel.CurrentContent = currentText;
        }

        ViewModel.IsLoadingContent = true;

        ViewModel.Tag = tag;
        string content = ViewModel.CurrentContent ?? String.Empty;
        SetContentToCurrentEditBox(content);

        if (ViewModel.Tag is "split" or "preview")
        {
            UpdateMarkdownPreview(content);
        }

        ViewModel.IsLoadingContent = false;
    }

    private void TabViewContent_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (!string.IsNullOrEmpty(ViewModel.CurrentContent))
        {
            SetContentToCurrentEditBox(ViewModel.CurrentContent);
            UpdateMarkdownPreview(ViewModel.CurrentContent);
        }
    }

    private void InitializeEditBoxes()
    {
        if (EditBox is not null)
        {
            EditBox.EnableSyntaxHighlighting = true;
            EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
            EditBox.DoAutoPairing = true;
        }

        if (EditBox_Split is not null)
        {
            EditBox_Split.EnableSyntaxHighlighting = true;
            EditBox_Split.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
            EditBox_Split.DoAutoPairing = true;
        }
    }

    private void EditBox_TextChanged(TextControlBox sender)
    {
        string text = sender.GetText();
        UpdateMarkdownPreview(text);
        ViewModel.CurrentContent = text;
    }

    private TextControlBox? CurrentEditBox => ViewModel.Tag switch
    {
        "md" => EditBox,
        "split" => EditBox_Split,
        _ => null
    };

    private WebView2? CurrentMarkdownPreview => ViewModel.Tag switch
    {
        "split" => MarkdownPreview_Split,
        "preview" => MarkdownPreview,
        _ => null
    };

    private string GetCurrentEditBoxText() => CurrentEditBox?.GetText() ?? string.Empty;

    public void SetContent(string text, string? fileName)
    {
        ViewModel.IsLoadingContent = true;
        ViewModel.FileName = fileName;
        ViewModel.SetOriginalContent(text);
        SetContentToCurrentEditBox(text);
        UpdateMarkdownPreview(text);
        ViewModel.IsLoadingContent = false;
    }

    private void SetContentToCurrentEditBox(string text) => CurrentEditBox?.LoadText(text);

    public string GetContent() => GetCurrentEditBoxText();

    public IEnumerable<string> GetContentToSaveFile() => CurrentEditBox?.Lines ?? [];

    public string GetFilePath() => ViewModel.FilePath ?? string.Empty;

    public string GetFileName() => ViewModel.FileName ?? string.Empty;

    public void SetFilePath(string filePath, string fileName) => ViewModel.SetFilePath(filePath, fileName);

    public bool IsDirty() => ViewModel.IsDirty;

    public void Undo() => CurrentEditBox?.Undo();

    public void Redo() => CurrentEditBox?.Redo();

    public void Cut() => CurrentEditBox?.Cut();

    public void Copy() => CurrentEditBox?.Copy();

    public void Paste() => CurrentEditBox?.Paste();

    public void MarkAsClean() => ViewModel.MarkAsClean();

    private async void InitializeWebViews()
    {
        try
        {
            if (MarkdownPreview_Split is not null)
            {
                await MarkdownPreview_Split.EnsureCoreWebView2Async();
                MarkdownPreview_Split.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
            }

            if (MarkdownPreview is not null)
            {
                await MarkdownPreview.EnsureCoreWebView2Async();
                MarkdownPreview.NavigateToString(GitHubPreview.GetEmptyPreviewHtml());
            }
        }
        catch { }
    }

    private void UpdateMarkdownPreview(string markdownText)
    {
        try
        {
            string html = ConvertMarkdownToHtml(markdownText);
            CurrentMarkdownPreview?.NavigateToString(html);
        }
        catch { }
    }

    private string ConvertMarkdownToHtml(string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return GitHubPreview.GetEmptyPreviewHtml();

        string htmlBody = Markdown.ToHtml(markdown, _markdownPipeline);

        return GitHubPreview.WrapWithGitHubStyle(htmlBody);
    }

    public void DisposeWebView()
    {
        MarkdownPreview_Split?.Close();
        MarkdownPreview?.Close();
    }

    public void Dispose()
    {
        ViewModel.Dispose();
        DisposeWebView();
    }
}