using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace InkMD.Core.Services;

/// <summary>
/// Provides document-level operations for reading/writing files.
/// This service is platform-agnostic and does not depend on any UI controls.
/// </summary>
public static class DocumentService
{
    /// <summary>
    /// Reads all text from a file path using the provided encoding (default UTF-8).
    /// </summary>
    public static string ReadAllText(string filePath, Encoding? encoding = null)
        => File.ReadAllText(filePath, encoding ?? Encoding.UTF8);

    /// <summary>
    /// Writes all lines to a file path using UTF-8 encoding.
    /// </summary>
    public static void WriteAllLines(string filePath, IEnumerable<string> lines)
        => File.WriteAllLines(filePath, lines, Encoding.UTF8);

    /// <summary>
    /// Detects BOM from byte array and decodes to string.
    /// </summary>
    public static string DetectAndDecode(ReadOnlySpan<byte> bytes)
    {
        ReadOnlySpan<byte> utf8Bom = [0xEF, 0xBB, 0xBF];
        ReadOnlySpan<byte> utf16LeBom = [0xFF, 0xFE];
        ReadOnlySpan<byte> utf16BeBom = [0xFE, 0xFF];

        if (bytes.Length >= 3 && bytes[..3].SequenceEqual(utf8Bom))
            return Encoding.UTF8.GetString(bytes[3..]);

        if (bytes.Length >= 2 && bytes[..2].SequenceEqual(utf16LeBom))
            return Encoding.Unicode.GetString(bytes[2..]);

        if (bytes.Length >= 2 && bytes[..2].SequenceEqual(utf16BeBom))
            return Encoding.BigEndianUnicode.GetString(bytes[2..]);

        try
        {
            return Encoding.UTF8.GetString(bytes);
        }
        catch
        {
            return Encoding.Default.GetString(bytes);
        }
    }
}