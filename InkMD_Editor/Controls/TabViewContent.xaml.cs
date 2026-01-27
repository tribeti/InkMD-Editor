using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messages;
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
            EditBox.SelectionChanged += (s, e) => UpdateFormattingState(EditBox);
        }

        if (EditBox_Split is not null)
        {
            EditBox_Split.EnableSyntaxHighlighting = true;
            EditBox_Split.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
            EditBox_Split.DoAutoPairing = true;
            EditBox_Split.SelectionChanged += (s, e) => UpdateFormattingState(EditBox_Split);
        }
    }

    private void EditBox_TextChanged(TextControlBox sender)
    {
        string text = sender.GetText();
        UpdateMarkdownPreview(text);
        ViewModel.CurrentContent = text;
        UpdateFormattingState(sender);
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

    public void ApplyBold() => ToggleFormattingStyle("**");

    public void ApplyItalic() => ToggleFormattingStyle("*");

    public void ApplyStrikethrough() => ToggleStrikethrough();

    private void ToggleFormattingStyle(string marker)
    {
        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        bool hasStrike = IsFormattedWith(text, "~~");
        string coreText = hasStrike ? text[2..^2] : text;
        string newText;

        if (IsWrappedWith(coreText, marker))
        {
            newText = coreText.Substring(marker.Length, coreText.Length - (marker.Length * 2));
        }
        else
        {
            newText = $"{marker}{coreText}{marker}";
        }

        if (hasStrike)
            newText = $"~~{newText}~~";

        ApplyTextChange(newText);
    }

    private void ToggleStrikethrough()
    {
        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        string newText = IsFormattedWith(text, "~~")
            ? text[2..^2] : $"~~{text}~~";
        ApplyTextChange(newText);
    }

    private bool IsWrappedWith(string text, string marker)
    {
        if (text.Length < marker.Length * 2)
            return false;

        if (marker == "*" && text.StartsWith("**") && text.EndsWith("**") && !text.StartsWith("***"))
            return false;

        return text.StartsWith(marker) && text.EndsWith(marker);
    }

    private string GetTextToFormat()
    {
        if (CurrentEditBox is null)
            return string.Empty;

        if (CurrentEditBox.HasSelection)
        {
            return CurrentEditBox.SelectedText ?? string.Empty;
        }

        try
        {
            int currentLine = CurrentEditBox.CurrentLineIndex;
            if (currentLine < 0 || currentLine >= CurrentEditBox.NumberOfLines)
                return string.Empty;

            return CurrentEditBox.GetLineText(currentLine) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void ApplyTextChange(string newText)
    {
        if (CurrentEditBox is null)
            return;
        try
        {
            if (CurrentEditBox.HasSelection)
                CurrentEditBox.SelectedText = newText;
            else
                CurrentEditBox.SetLineText(CurrentEditBox.CurrentLineIndex, newText);

            UpdateFormattingState(CurrentEditBox);
        }
        catch { }
    }

    private bool IsFormattedWith(string text, string marker)
    {
        return !string.IsNullOrEmpty(text) &&
               text.StartsWith(marker) &&
               text.EndsWith(marker) &&
               text.Length >= 2 * marker.Length;
    }

    private void UpdateFormattingState(TextControlBox sender)
    {
        if (sender is null)
            return;

        string text = GetTextToFormat();
        string textWithoutStrikethrough = text;
        bool hasStrikethrough = false;

        if (text.StartsWith("~~") && text.EndsWith("~~") && text.Length > 4)
        {
            textWithoutStrikethrough = text[2..^2];
            hasStrikethrough = true;
        }

        bool hasBoldItalic = IsFormattedWith(textWithoutStrikethrough, "***");
        bool hasBold = IsFormattedWith(textWithoutStrikethrough, "**") || hasBoldItalic;
        bool hasItalic = (IsFormattedWith(textWithoutStrikethrough, "*") && !IsFormattedWith(textWithoutStrikethrough, "**")) || hasBoldItalic;

        ViewModel.IsBoldActive = hasBold;
        ViewModel.IsItalicActive = hasItalic;
        ViewModel.IsStrikethroughActive = hasStrikethrough;

        WeakReferenceMessenger.Default.Send(new FormattingStateMessage(
            ViewModel.IsBoldActive,
            ViewModel.IsItalicActive,
            ViewModel.IsStrikethroughActive
        ));
    }

    public void MarkAsClean() => ViewModel.MarkAsClean();

    public void InsertText(string text)
    {
        if (CurrentEditBox is null)
            return;

        CurrentEditBox.AddLine(CurrentEditBox.CurrentLineIndex, text);
    }

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