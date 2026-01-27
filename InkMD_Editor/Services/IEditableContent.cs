using System.Collections.Generic;

namespace InkMD_Editor.Services;

public interface IEditableContent
{
    string GetContent();
    string GetFilePath();
    string GetFileName();
    void SetFilePath(string filePath, string fileName);
    void SetContent(string content, string? fileName);
    void Undo();
    void Redo();
    void Cut();
    void Copy();
    void Paste();
    void ApplyBold();
    void ApplyItalic();
    void ApplyStrikethrough();
    bool IsDirty();
    void MarkAsClean();
    IEnumerable<string> GetContentToSaveFile();
    void InsertText(string text);
}