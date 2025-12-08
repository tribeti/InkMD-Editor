using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl
{
    private readonly FileService _fileService = new();
    private readonly TemplateService _templateService = new();
    private readonly DialogService _dialogService = new();

    public MainMenu ()
    {
        InitializeComponent();
    }

    [RelayCommand]
    private async Task OpenFile ()
    {
        var storageFile = await _fileService.OpenFileAsync();
        if ( storageFile is not null )
        {
            WeakReferenceMessenger.Default.Send(new FileOpenedMessage(storageFile));
        }
    }

    [RelayCommand]
    private async Task OpenFolder ()
    {
        var storageFolder = await _fileService.OpenFolderAsync();
        if ( storageFolder is not null )
        {
            WeakReferenceMessenger.Default.Send(new FolderOpenedMessage(storageFolder));
        }
    }

    [RelayCommand]
    private void Save ()
    {
        WeakReferenceMessenger.Default.Send(new SaveFileMessage(isNewFile: false));
    }

    [RelayCommand]
    private async Task SaveAsFile ()
    {
        var filePath = await _fileService.SaveFileAsync();
        if ( filePath is not null )
        {
            WeakReferenceMessenger.Default.Send(new SaveFileRequestMessage(filePath));
        }
    }

    [RelayCommand]
    private static void ExitApplication ()
    {
        App.Current.Exit();
    }

    public void SetVisibility (bool isVisible)
    {
        DisplayMode.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        DisplayMode.SelectedIndex = 1;
    }

    private async void TemplateFlyout_Opening (object sender , object e)
    {
        await LoadTemplatesAsync();
    }

    private async Task LoadTemplatesAsync ()
    {
        try
        {
            var templates = await _templateService.GetAllTemplatesAsync();
            TemplateGridView.ItemsSource = templates;
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync("Không thể load templates" + ex.Message);
        }
    }

    private async void TemplateGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( e.AddedItems.Count > 0 && e.AddedItems [0] is TemplateInfo selectedTemplate )
        {
            try
            {
                var content = await _templateService.LoadTemplateAsync(selectedTemplate.FileName);
                WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content));
                TemplateFlyout.Hide();
                TemplateGridView.SelectedItem = null;
            }
            catch ( Exception ex )
            {
                await _dialogService.ShowErrorAsync("Không thể load templates" + ex.Message);
            }
        }
    }
}
