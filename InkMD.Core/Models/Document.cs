using System;
using System.Text;

namespace InkMD.Core.Models;

// this is for document representation in the editor
public class Document
{
    public string FilePath { get; set; }
    public string FileName => string.IsNullOrEmpty(FilePath) ? "Untitled" : System.IO.Path.GetFileName(FilePath);
    public string Content { get; set; } = string.Empty;
    public bool IsDirty { get; set; } = false;
    public bool IsReadOnly { get; set; } = false;
    public Encoding FileEncoding { get; set; } = Encoding.UTF8;
    public LineEndingType LineEnding { get; set; } = LineEndingType.CRLF;
    public DateTime LastModifiedOnDisk { get; set; }
}

public enum LineEndingType { CRLF, LF, CR }