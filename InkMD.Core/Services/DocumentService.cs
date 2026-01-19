using System.IO;
using TextControlBoxNS;

namespace InkMD.Core.Services;

public class DocumentService
{
    public string GetText(TextControlBox textBox) => textBox.GetText() ?? string.Empty;
    public void SetText(TextControlBox textBox, string text) => textBox.LoadText(text);
    public void InsertText(TextControlBox textBox, string text) => textBox.AddLine(textBox.CurrentLineIndex + 1, text);
    public void MarkAsDirty(TextControlBox textBox)
    {

    }
    public void SaveDocument(TextControlBox textBox)
    {
        File.WriteAllLines("file.txt", textBox.Lines);
    }
    public void NotifyChanges() { }
}