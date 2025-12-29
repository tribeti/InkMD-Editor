namespace InkMD_Editor.Interfaces;

public interface IEditableContent
{
    string GetContent ();
    string GetFilePath ();
    string GetFileName ();
    void SetFilePath (string filePath , string fileName);
    void SetContent (string content , string? fileName);
    void Undo ();
    void Redo ();
    void Cut ();
    void Copy ();
    void Paste ();
}