using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messages;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using TextControlBoxNS;

namespace InkMD_Editor.Controls;

public sealed partial class EditTabViewContent : UserControl, IEditableContent
{
    public TabViewContentViewModel ViewModel { get; } = new();

    public EditTabViewContent()
    {
        InitializeComponent();
        EditBox.EnableSyntaxHighlighting = true;
        EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
        EditBox.SelectionChanged += (s, e) => UpdateFormattingState(EditBox);
    }

    public void SetContent(string text, string? fileName)
    {
        ViewModel.IsLoadingContent = true;
        ViewModel.FileName = fileName;
        ViewModel.SetOriginalContent(text);
        EditBox.LoadText(text);
        ViewModel.IsLoadingContent = false;
    }

    public string GetContent() => EditBox.GetText() ?? string.Empty;

    public IEnumerable<string> GetContentToSaveFile() => EditBox.Lines ?? [];

    public string GetFilePath() => ViewModel.FilePath ?? string.Empty;

    public string GetFileName() => ViewModel.FileName ?? string.Empty;

    public void SetFilePath(string filePath, string fileName) => ViewModel.SetFilePath(filePath, fileName);

    public void Undo() => EditBox?.Undo();

    public void Redo() => EditBox?.Redo();

    public void Cut() => EditBox?.Cut();

    public void Copy() => EditBox?.Copy();

    public void Paste() => EditBox?.Paste();

    public void ApplyBold()
    {
        if (EditBox is null)
            return;

        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        // If it has bold+italic (***), toggle to italic (*)
        if (IsFormattedWith(text, "***"))
        {
            RemoveFormatting(text, "***");
            ApplyFormatting("*");
        }
        // If it has bold (**), remove it
        else if (IsFormattedWith(text, "**"))
        {
            RemoveFormatting(text, "**");
        }
        // If it has italic (*), toggle to bold+italic (***)
        else if (IsFormattedWith(text, "*") && !IsFormattedWith(text, "**"))
        {
            RemoveFormatting(text, "*");
            ApplyFormatting("***");
        }
        // No formatting, apply bold
        else
        {
            ApplyFormatting("**");
        }
        UpdateFormattingState(EditBox);
    }

    public void ApplyItalic()
    {
        if (EditBox is null)
            return;

        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        // If it has bold+italic (***), toggle to bold (**)
        if (IsFormattedWith(text, "***"))
        {
            RemoveFormatting(text, "***");
            ApplyFormatting("**");
        }
        // If it has italic (*), remove it
        else if (IsFormattedWith(text, "*") && !IsFormattedWith(text, "**"))
        {
            RemoveFormatting(text, "*");
        }
        // If it has bold (**), toggle to bold+italic (***)
        else if (IsFormattedWith(text, "**"))
        {
            RemoveFormatting(text, "**");
            ApplyFormatting("***");
        }
        // No formatting, apply italic
        else
        {
            ApplyFormatting("*");
        }
        UpdateFormattingState(EditBox);
    }

    public void ApplyStrikethrough()
    {
        if (EditBox is null)
            return;

        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        // Check if text already has strikethrough
        if (HasStrikethrough(text))
        {
            // Remove strikethrough but preserve bold/italic
            RemoveStrikethrough(text);
        }
        else
        {
            // Add strikethrough while preserving any existing bold/italic
            AddStrikethrough(text);
        }
        UpdateFormattingState(EditBox);
    }

    private bool HasStrikethrough(string text)
    {
        return !string.IsNullOrEmpty(text) && text.StartsWith("~~") && text.EndsWith("~~");
    }

    private void AddStrikethrough(string text)
    {
        if (EditBox is null)
            return;

        try
        {
            if (EditBox.HasSelection)
            {
                EditBox.SurroundSelectionWith("~~");
            }
            else
            {
                int lineIndex = EditBox.CurrentLineIndex;
                if (lineIndex < 0 || lineIndex >= EditBox.NumberOfLines)
                    return;

                string lineText = EditBox.GetLineText(lineIndex) ?? string.Empty;
                string strikeThroughLine = $"~~{lineText}~~";
                EditBox.SetLineText(lineIndex, strikeThroughLine);
            }
        }
        catch
        {
            // Silently handle errors
        }
    }

    private void RemoveStrikethrough(string text)
    {
        if (EditBox is null)
            return;

        try
        {
            // Extract content between ~~ markers
            string unformatted = text.StartsWith("~~") && text.EndsWith("~~")
                ? text.Substring(2, text.Length - 4)
                : text;

            int lineIndex = EditBox.CurrentLineIndex;
            if (lineIndex >= 0 && lineIndex < EditBox.NumberOfLines)
            {
                EditBox.SetLineText(lineIndex, unformatted);
            }
        }
        catch
        {
            // Silently handle errors
        }
    }

    private string GetTextToFormat()
    {
        if (EditBox is null)
            return string.Empty;

        if (EditBox.HasSelection)
        {
            return EditBox.SelectedText ?? string.Empty;
        }

        try
        {
            int currentLine = EditBox.CurrentLineIndex;
            if (currentLine < 0 || currentLine >= EditBox.NumberOfLines)
                return string.Empty;

            return EditBox.GetLineText(currentLine) ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    private void ApplyFormatting(string marker)
    {
        if (EditBox is null)
            return;

        try
        {
            if (EditBox.HasSelection)
            {
                EditBox.SurroundSelectionWith(marker);
            }
            else
            {
                int lineIndex = EditBox.CurrentLineIndex;
                if (lineIndex < 0 || lineIndex >= EditBox.NumberOfLines)
                    return;

                string lineText = EditBox.GetLineText(lineIndex) ?? string.Empty;
                string formattedLine = $"{marker}{lineText}{marker}";
                EditBox.SetLineText(lineIndex, formattedLine);
            }
        }
        catch
        {
            // Silently handle formatting errors
        }
    }

    private void RemoveFormatting(string text, string marker)
    {
        if (EditBox is null)
            return;

        try
        {
            string unformatted = text.StartsWith(marker) && text.EndsWith(marker)
                ? text.Substring(marker.Length, text.Length - 2 * marker.Length)
                : text;

            int lineIndex = EditBox.CurrentLineIndex;
            if (lineIndex >= 0 && lineIndex < EditBox.NumberOfLines)
            {
                EditBox.SetLineText(lineIndex, unformatted);
            }
        }
        catch
        {
            // Silently handle formatting errors
        }
    }

    private bool IsFormattedWith(string text, string marker)
    {
        return !string.IsNullOrEmpty(text) &&
               text.StartsWith(marker) &&
               text.EndsWith(marker) &&
               text.Length > 2 * marker.Length;
    }

    private void UpdateFormattingState(TextControlBox sender)
    {
        if (sender is null)
            return;

        string text = GetTextToFormat();

        // Strip strikethrough markers first to check for bold/italic
        string textWithoutStrikethrough = text;
        if (text.StartsWith("~~") && text.EndsWith("~~"))
        {
            textWithoutStrikethrough = text.Substring(2, text.Length - 4);
        }

        // Check for bold+italic (3 stars: ***)
        bool hasBoldItalic = IsFormattedWith(textWithoutStrikethrough, "***");

        // Check for bold (2 stars: **)
        bool hasBold = IsFormattedWith(textWithoutStrikethrough, "**") || hasBoldItalic;

        // Check for italic (1 star: *), but exclude if it's bold or bold+italic
        bool hasItalic = IsFormattedWith(textWithoutStrikethrough, "*") && !IsFormattedWith(textWithoutStrikethrough, "**");

        // Strikethrough is checked independently
        bool hasStrikethrough = text.StartsWith("~~") && text.EndsWith("~~") && text.Length > 4;

        ViewModel.IsBoldActive = hasBold;
        ViewModel.IsItalicActive = hasItalic || hasBoldItalic;
        ViewModel.IsStrikethroughActive = hasStrikethrough;

        WeakReferenceMessenger.Default.Send(new FormattingStateMessage(
            ViewModel.IsBoldActive,
            ViewModel.IsItalicActive,
            ViewModel.IsStrikethroughActive
        ));
    }

    public bool IsDirty() => ViewModel.IsDirty;

    public void MarkAsClean() => ViewModel.MarkAsClean();

    public void InsertText(string text)
    {
        if (EditBox is null)
            return;

        EditBox.AddLine(EditBox.CurrentLineIndex, text);
    }

    private void EditBox_TextChanged(TextControlBox sender)
    {
        ViewModel.CurrentContent = sender.GetText();
        UpdateFormattingState(sender);
    }

    public void Dispose()
    {
        ViewModel.Dispose();
    }
}
