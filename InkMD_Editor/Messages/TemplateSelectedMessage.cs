namespace InkMD_Editor.Messages;

public class TemplateSelectedMessage (string content , bool createNewFile = false)
{
    public string Content { get; } = content;
    public bool CreateNewFile { get; } = createNewFile;
}