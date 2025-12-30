using System.Threading.Tasks;

namespace InkMD_Editor.Services;

public interface IDialogService
{
    Task ShowErrorAsync (string message);
    Task ShowSuccessAsync (string message);
    Task<bool> ShowConfirmationAsync (string message);
}
