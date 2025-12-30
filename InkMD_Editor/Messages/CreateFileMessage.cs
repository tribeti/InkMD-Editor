namespace InkMD_Editor.Messages;

public record CreateFileMessage (
    string FileName ,
    bool IsMarkdown ,
    string? FilePath = null
);
