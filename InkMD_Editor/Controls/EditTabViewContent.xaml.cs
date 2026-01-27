using InkMD_Editor.Messages;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using TextControlBoxNS;
using CommunityToolkit.Mvvm.Messaging;

namespace InkMD_Editor.Controls;

public sealed partial class EditTabViewContent : UserControl, IEditableContent
{
    public TabViewContentViewModel ViewModel { get; } = new();

    public EditTabViewContent()
    {
        InitializeComponent();
        EditBox.EnableSyntaxHighlighting = true;
        EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
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

        if (IsFormattedWith(text, "**"))
        {
            RemoveFormatting(text, "**");
        }
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

        if (IsFormattedWith(text, "*"))
        {
            RemoveFormatting(text, "*");
        }
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

        if (IsFormattedWith(text, "~~"))
        {
            RemoveFormatting(text, "~~");
        }
        else
        {
            ApplyFormatting("~~");
        }
        UpdateFormattingState(EditBox);
    }

    private string GetTextToFormat()
    {
        if (EditBox is null)
            return string.Empty;

        if (EditBox.HasSelection)
        {
            return EditBox.SelectedText ?? string.Empty;
        }

        int currentLine = EditBox.CurrentLineIndex;
        return EditBox.GetLineText(currentLine) ?? string.Empty;
    }

    private void ApplyFormatting(string marker)
    {
        if (EditBox is null)
            return;

        if (EditBox.HasSelection)
        {
            EditBox.SurroundSelectionWith(marker);
        }
        else
        {
            int lineIndex = EditBox.CurrentLineIndex;
            string lineText = EditBox.GetLineText(lineIndex) ?? string.Empty;
            string formattedLine = $"{marker}{lineText}{marker}";
            EditBox.SetLineText(lineIndex, formattedLine);
        }
    }

    private void RemoveFormatting(string text, string marker)
    {
        if (EditBox is null)
            return;

        string unformatted = text.StartsWith(marker) && text.EndsWith(marker)
            ? text.Substring(marker.Length, text.Length - 2 * marker.Length)
            : text;

        int lineIndex = EditBox.CurrentLineIndex;
        EditBox.SetLineText(lineIndex, unformatted);
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
        
        ViewModel.IsBoldActive = IsFormattedWith(text, "**");
        ViewModel.IsItalicActive = IsFormattedWith(text, "*") && !IsFormattedWith(text, "**");
        ViewModel.IsStrikethroughActive = IsFormattedWith(text, "~~");

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
