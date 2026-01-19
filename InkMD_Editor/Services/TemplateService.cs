using InkMD_Editor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel;
using Windows.Storage;

namespace InkMD_Editor.Services;

public static class TemplateService
{
    private const string TemplatesPath = @"Assets\Templates";
    private const string IconsPath = @"Assets\Icons";

    public static Task<string> LoadTemplateAsync(string fileName) => ReadFileContentAsync(TemplatesPath, fileName, "Template");

    public static async Task<List<MdTemplate>> GetAllTemplatesAsync()
    {
        try
        {
            var files = await GetFilesFromFolderAsync(TemplatesPath, [".md"]);

            return files.Select(file => new MdTemplate(
                file.Name,
                Path.GetFileNameWithoutExtension(file.Name),
                file.Path
            )).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading templates: {ex.Message}", ex);
        }
    }

    public static async Task<List<IconItem>> GetAllIconsAsync()
    {
        try
        {
            var files = await GetFilesFromFolderAsync(IconsPath, [".svg"]);

            return files.OrderBy(f => f.Name)
                        .Select(file => new IconItem(
                            Path.GetFileNameWithoutExtension(file.Name).ToLower(),
                            file.Path,
                            file.Name
                        )).ToList();
        }
        catch (Exception ex)
        {
            throw new Exception($"Error loading icons: {ex.Message}", ex);
        }
    }

    private static async Task<string> ReadFileContentAsync(string folderPath, string fileName, string itemType)
    {
        try
        {
            var folder = Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync(Path.Combine(folderPath, fileName));
            return await FileIO.ReadTextAsync(file);
        }
        catch (FileNotFoundException)
        {
            throw new Exception($"{itemType} '{fileName}' not found in {folderPath}!");
        }
    }

    private static async Task<IEnumerable<StorageFile>> GetFilesFromFolderAsync(string folderPath, string[] allowedExtensions)
    {
        var folder = Package.Current.InstalledLocation;
        var targetFolder = await folder.GetFolderAsync(folderPath);
        var files = await targetFolder.GetFilesAsync();

        return files.Where(f => allowedExtensions.Any(ext =>
            f.FileType.Equals(ext, StringComparison.OrdinalIgnoreCase)));
    }
}