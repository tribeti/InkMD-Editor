using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace InkMD_Editor.Services;

public class DialogService : IDialogService
{
    private XamlRoot? _xamlRoot;

    public void SetXamlRoot (XamlRoot xamlRoot) => _xamlRoot = xamlRoot;

    public async Task ShowErrorAsync (string message)
    {
        if ( _xamlRoot is null )
            return;
        var dialog = new ContentDialog
        {
            Title = "Error" ,
            Content = message ,
            CloseButtonText = "OK" ,
            XamlRoot = _xamlRoot
        };
        await dialog.ShowAsync();
    }

    public async Task ShowSuccessAsync (string message)
    {
        if ( _xamlRoot is null )
            return;
        var dialog = new ContentDialog
        {
            Title = "Done" ,
            Content = message ,
            CloseButtonText = "OK" ,
            XamlRoot = _xamlRoot
        };
        await dialog.ShowAsync();
    }

    public async Task<bool> ShowConfirmationAsync (string message)
    {
        if ( _xamlRoot is null )
            return false;

        var dialog = new ContentDialog
        {
            Title = "Confirm" ,
            Content = message ,
            PrimaryButtonText = "Yes" ,
            CloseButtonText = "No" ,
            DefaultButton = ContentDialogButton.Primary ,
            XamlRoot = _xamlRoot
        };
        ContentDialogResult result = await dialog.ShowAsync();
        return result == ContentDialogResult.Primary;
    }
}