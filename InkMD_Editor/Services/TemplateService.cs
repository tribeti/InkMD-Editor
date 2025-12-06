using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Services;

public class TemplateService
{
    // Load nội dung 1 template
    public async Task<string> LoadTemplateAsync (string fileName)
    {
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync($"Assets\\Templates\\{fileName}");
            return await FileIO.ReadTextAsync(file);
        }
        catch ( FileNotFoundException )
        {
            throw new Exception($"Template '{fileName}' không tìm thấy!");
        }
    }

    // Load SVG icon
    public async Task<string> LoadIconAsync (string iconName)
    {
        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var file = await folder.GetFileAsync($"Assets\\Templates\\Icons\\{iconName}");
            return await FileIO.ReadTextAsync(file);
        }
        catch ( FileNotFoundException )
        {
            throw new Exception($"Icon '{iconName}' không tìm thấy!");
        }
    }

    // Lấy danh sách tất cả templates
    public async Task<List<TemplateInfo>> GetAllTemplatesAsync ()
    {
        var templates = new List<TemplateInfo>();

        try
        {
            var folder = Windows.ApplicationModel.Package.Current.InstalledLocation;
            var templatesFolder = await folder.GetFolderAsync("Assets\\Templates");
            var files = await templatesFolder.GetFilesAsync();

            foreach ( var file in files.Where(f => f.FileType == ".md") )
            {
                templates.Add(new TemplateInfo
                {
                    FileName = file.Name ,
                    DisplayName = Path.GetFileNameWithoutExtension(file.Name) ,
                    Path = file.Path
                });
            }
        }
        catch ( Exception ex )
        {
            System.Diagnostics.Debug.WriteLine($"Lỗi load templates: {ex.Message}");
        }

        return templates;
    }
}

// Model cho template info
public class TemplateInfo
{
    public string FileName { get; set; }
    public string DisplayName { get; set; }
    public string Path { get; set; }
}