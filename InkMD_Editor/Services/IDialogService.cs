using System.Threading.Tasks;

namespace InkMD_Editor.Services;

public interface IDialogService
{
    void SetXamlRoot (Microsoft.UI.Xaml.XamlRoot xamlRoot);
    Task ShowErrorAsync (string message);
    Task ShowSuccessAsync (string message);
    Task<bool> ShowConfirmationAsync (string message);
}
