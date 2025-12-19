using InkMD_Editor.Helpers;
using InkMD_Editor.Interfaces;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Xaml.Controls;
using System;

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

        EditBox.EditBox.TextChanged += OnEditBoxTextChanged;
    }

    private void OnEditBoxTextChanged (object sender)
    {
        string currentText = EditBox.GetContent();
        ViewModel.CurrentContent = currentText;
        UpdateMarkdownPreview(currentText);
    }

    public void SetContent (string text , string? fileName)
    {
        EditBox.SetContent(text , fileName);
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
        UpdateMarkdownPreview(text);
    }

    public string GetContent ()
    {
        return EditBox.GetContent();
    }

    public string GetFilePath () => EditBox.GetFilePath();

    public string GetFileName () => EditBox.GetFileName();

    public void SetFilePath (string filePath , string fileName)
    {
        EditBox.SetFilePath(filePath , fileName);
        ViewModel.SetFilePath(filePath , fileName);
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