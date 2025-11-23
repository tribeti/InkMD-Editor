using Windows.Storage;

namespace InkMD_Editor.Messagers;

public class FileOpenedMessage
{
    public StorageFile File { get; }

    public FileOpenedMessage (StorageFile file)
    {
        File = file;
    }
}

public class FolderOpenedMessage
{
    public StorageFolder Folder { get; }

    public FolderOpenedMessage (StorageFolder folder)
    {
        Folder = folder;
    }
}

public class ErrorMessage
{
    public string Message { get; }

    public ErrorMessage (string message)
    {
        Message = message;
    }
}

public class GetEditorContentMessage
{
    public string? Content { get; set; }
    public string? FileName { get; set; }
}

public class SaveFileMessage
{
    public bool IsNewFile { get; set; }

    public SaveFileMessage (bool isNewFile = false)
    {
        IsNewFile = isNewFile;
    }
}

public class SaveAsFileMessage
{
}

public class SaveFileRequestMessage
{
    public string FilePath { get; }

    public SaveFileRequestMessage (string filePath)
    {
        FilePath = filePath;
    }
}

public class FileSavedMessage
{
    public string FilePath { get; }
    public string FileName { get; }

    public FileSavedMessage (string filePath , string fileName)
    {
        FilePath = filePath;
        FileName = fileName;
    }
}
