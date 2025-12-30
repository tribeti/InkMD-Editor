namespace InkMD_Editor.Messages
{
    public class CreateFileMessage (string fileName , bool isMarkdown , string? filePath = null)
    {
        public string FileName { get; } = fileName;
        public bool IsMarkdown { get; } = isMarkdown;
        public string? FilePath { get; } = filePath;
    }
}