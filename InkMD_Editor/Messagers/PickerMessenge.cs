using Windows.Storage;

namespace InkMD_Editor.Messagers;

public class FileOpenedMessage (StorageFile file)
{
    public StorageFile File { get; } = file;
}

public class FolderOpenedMessage (StorageFolder folder)
{
    public StorageFolder Folder { get; } = folder;
}

public class ErrorMessage (string message)
{
    public string Message { get; } = message;
}

public class SaveFileMessage (bool isNewFile = false)
{
    public bool IsNewFile { get; set; } = isNewFile;
}

public class SaveFileRequestMessage (string filePath)
{
    public string FilePath { get; } = filePath;
}

public class FileSavedMessage (string filePath , string fileName)
{
    public string FilePath { get; } = filePath;
    public string FileName { get; } = fileName;
}
