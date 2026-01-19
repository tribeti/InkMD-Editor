using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Services;

public interface IFileService
{
    Task<StorageFolder?> OpenFolderAsync();
    Task<StorageFile?> OpenFileAsync();
    Task<string?> SaveFileAsync();
    Task<StorageFile?> CreateNewFileAsync(string suggestedName, string? extension);
    Task<StorageFile?> CreateFileDirectlyAsync(string fileName, string extension);
}
