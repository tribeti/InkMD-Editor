using System.Collections.ObjectModel;

namespace InkMD.Core.Models;

public class FileSystemItem
{
    public string FullPath { get; set; }

    public string Name => string.IsNullOrEmpty(FullPath)
        ? "My Computer"
        : System.IO.Path.GetFileName(FullPath);
    public ItemType Type { get; set; }
    public ObservableCollection<FileSystemItem> Children { get; set; } = [];
    public bool HasDummyChild { get; set; } = false;
    public bool IsExpanded { get; set; } = false;
    //public string Icon
    //{
    //get
    //{
    //    if (Type == ItemType.Folder)
    //        return IsExpanded ? "FolderOpenIcon" : "FolderIcon";
    //    return "FileIcon";
    //}
    //}
}

public enum ItemType
{
    File,
    Folder,
    Drive
}