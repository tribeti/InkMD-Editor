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

    public bool IsDirty() => ViewModel.IsDirty;

    public void MarkAsClean() => ViewModel.MarkAsClean();

    private void EditBox_TextChanged(TextControlBox sender)
    {
        ViewModel.CurrentContent = sender.GetText();
    }

    public void Dispose()
    {
        ViewModel.Dispose();
    }
}
