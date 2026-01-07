using InkMD_Editor.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Services;

public class TemplateService
{
    public static async Task<string> LoadTemplateAsync (string fileName)
    {
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync($"Assets\\Templates\\{fileName}");
            return await FileIO.ReadTextAsync(file);
        }
        catch ( FileNotFoundException )
        {
            throw new Exception($"Template '{fileName}' not found!");
        }
    }

    public static async Task<string> LoadIconAsync (string iconName)
    {
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync($"Assets\\Icons\\{iconName}");
            return await FileIO.ReadTextAsync(file);
        }
        catch ( FileNotFoundException )
        {
            throw new Exception($"Icon '{iconName}' not found!");
        }
    }

    public static async Task<List<MdTemplate>> GetAllTemplatesAsync ()
    {
        var templates = new List<MdTemplate>();
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var templatesFolder = await folder.GetFolderAsync("Assets\\Templates");
            var files = await templatesFolder.GetFilesAsync();

            foreach ( var file in files.Where(f => f.FileType == ".md") )
            {
                templates.Add(new MdTemplate
                {
                    FileName = file.Name ,
                    DisplayName = Path.GetFileNameWithoutExtension(file.Name) ,
                    Path = file.Path
                });
            }
        }
        catch ( Exception ex )
        {
            throw new Exception($"Error load icons: {ex.Message}" , ex);
        }

        return templates;
    }

    public static async Task<List<IconItem>> GetAllIconsAsync ()
    {
        var icons = new List<IconItem>();
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var iconsFolder = await folder.GetFolderAsync("Assets\\Icons");
            var files = await iconsFolder.GetFilesAsync();
            var imageExtensions = new [] { ".svg" };

            foreach ( var file in files.Where(f => imageExtensions.Contains(f.FileType.ToLower())).OrderBy(f => f.Name) )
            {
                string nameWithoutExtension = Path.GetFileNameWithoutExtension(file.Name).ToLower();
                icons.Add(new IconItem
                {
                    Name = nameWithoutExtension ,
                    ImagePath = file.Path ,
                    FileName = file.Name
                });
            }
        }
        catch ( Exception ex )
        {
            throw new Exception($"Error load icons: {ex.Message}" , ex);
        }

        return icons;
    }
}