namespace InkMD_Editor.Messages;

public class ContentChangedMessage (string filePath , bool isDirty)
{
    public string FilePath { get; } = filePath;
    public bool IsDirty { get; } = isDirty;
}