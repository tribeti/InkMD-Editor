namespace InkMD.Core.Models;

public class File
{
    public string FullPath { get; set; }
    public string Name => string.IsNullOrEmpty(FullPath) ? "Tree Object" : System.IO.Path.GetFileName(FullPath);
}