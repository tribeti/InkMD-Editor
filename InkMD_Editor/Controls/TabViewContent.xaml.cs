using InkMD_Editor.Helpers;
using InkMD_Editor.Interfaces;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
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
    }

    private void MdEditor_TextChanged (object sender , RoutedEventArgs e)
    {
        var doc = MdEditor.Document;
        doc.GetText(TextGetOptions.None , out string text);
        UpdateMarkdownPreview(text);
        ViewModel.CurrentContent = text;
    }

    public void SetContent (string text , string? fileName)
    {
        var doc = MdEditor.Document;
        doc.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
        ViewModel.CurrentContent = text;
        UpdateMarkdownPreview(text);
    }

    public string GetContent ()
    {
        try
        {
            if ( MdEditor is null )
            {
                return ViewModel.CurrentContent ?? string.Empty;
            }

            var doc = MdEditor.Document;
            if ( doc is null )
            {
                return ViewModel.CurrentContent ?? string.Empty;
            }

            doc.GetText(TextGetOptions.None , out string text);
            return text ?? string.Empty;
        }
        catch ( Exception )
        {
            return ViewModel.CurrentContent ?? string.Empty;
        }
    }

    public string GetFilePath () => ViewModel.FilePath ?? string.Empty;

    public string GetFileName () => ViewModel.FileName ?? string.Empty;

    public void SetFilePath (string filePath , string fileName) => ViewModel.SetFilePath(filePath , fileName);

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