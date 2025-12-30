using Windows.Storage;

namespace InkMD_Editor.Messages;

public record FileOpenedMessage (StorageFile File);

public record FolderOpenedMessage (StorageFolder Folder);

public class ErrorMessage (string message)
{
    public string Message { get; } = message;
}

public record SaveFileMessage (bool IsNewFile = false);

public record SaveFileRequestMessage (string FilePath);

public record FileSavedMessage (string FilePath , string FileName);
