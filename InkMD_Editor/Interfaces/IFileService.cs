using System.Threading.Tasks;
using Windows.Storage;

namespace InkMD_Editor.Interfaces;

public interface IFileService
{
    Task<StorageFolder?> OpenFolderAsync ();
    Task<StorageFile?> OpenFileAsync ();
    Task<string?> SaveFileAsync ();
}
