using InkMD_Editor.Models;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl
{
    private readonly DialogService _dialogService = new();
    private MainMenuViewModel ViewModel { get; set; } = new();
    public event EventHandler<int>? ViewModeChanged;

    public MainMenu ()
    {
        InitializeComponent();
        DataContext = ViewModel;
        Unloaded += (s , e) => Dispose();
    }

    public void SetVisibility (bool isVisible , int selectedIndex = 1)
    {
        DisplayMode.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        if ( isVisible )
        {
            DisplayMode.SelectedIndex = selectedIndex;
        }
    }

    public void UpdateVisibilityForTab (object? tabContent)
    {
        if ( tabContent is null )
        {
            SetVisibility(false);
            return;
        }

        bool isMarkdown = tabContent is TabViewContent;
        SetVisibility(isMarkdown);
    }


    private async void TemplateFlyout_Opening (object sender , object e)
    {
        try
        {
            await ViewModel.LoadTemplatesCommand.ExecuteAsync(null);
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Cannot load template: {ex.Message}");
        }
    }

    private async void TemplateGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( e.AddedItems.Count > 0 && e.AddedItems [0] is MdTemplate selectedTemplate )
        {
            await HandleTemplateSelection(selectedTemplate);
        }
    }

    private async Task HandleTemplateSelection (MdTemplate template)
    {
        try
        {
            var content = await ViewModel.LoadTemplateContentAsync(template.FileName);
            if ( content is null )
            {
                await _dialogService.ShowErrorAsync("Cannot load template content");
                return;
            }

            TemplateFlyout.Hide();
            TemplateGridView.SelectedItem = null;
            await ShowTemplatePreviewDialog(template.DisplayName , content);
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Cannot load template: {ex.Message}");
        }
    }

    private async Task ShowTemplatePreviewDialog (string templateName , string content)
    {
        if ( TemplateDialog is null || previewWebView is null )
        {
            await _dialogService.ShowErrorAsync("Error: Cannot load dialog");
            return;
        }

        TemplateDialog.Title = $"Template Preview: {templateName}";
        TemplateDialog.XamlRoot = XamlRoot;

        try
        {
            await InitializeWebViewAsync();
            var html = ViewModel.ConvertMarkdownToHtml(content);
            previewWebView.NavigateToString(html);
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Cannot show preview: {ex.Message}");
            return;
        }

        var result = await TemplateDialog.ShowAsync();
        CleanupWebView();

        if ( result is ContentDialogResult.Primary )
        {
            ViewModel.SendTemplateSelectedMessage(content , createNewFile: true);
        }
        else if ( result is ContentDialogResult.Secondary )
        {
            ViewModel.SendTemplateSelectedMessage(content , createNewFile: false);
        }
    }

    private async void AppBarButton_Click (object sender , RoutedEventArgs e)
    {
        ViewModel.ClearSelectedIcons();
        IconGridView.SelectedItems.Clear();
        try
        {
            await ViewModel.LoadIconsCommand.ExecuteAsync(null);
            IconsDialog.XamlRoot = XamlRoot;
            IconsDialog.DefaultButton = ContentDialogButton.Primary;
            await IconsDialog.ShowAsync();
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Cannot load icon: {ex.Message}");
        }
    }

    private void IconGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        foreach ( var item in e.AddedItems )
        {
            if ( item is IconItem icon )
            {
                ViewModel.AddSelectedIcon(icon.Name);
            }
        }

        foreach ( var item in e.RemovedItems )
        {
            if ( item is IconItem icon )
            {
                ViewModel.RemoveSelectedIcon(icon.Name);
            }
        }
    }

    private async void CopyBtn_Click (object sender , RoutedEventArgs e)
    {
        var contentToCopy = CodeDisplay.Text;

        if ( string.IsNullOrEmpty(contentToCopy) )
        {
            return;
        }

        var dataPackage = new DataPackage();
        dataPackage.SetText(contentToCopy);
        Clipboard.SetContent(dataPackage);

        await ShowCopyFeedback();
    }

    private async Task ShowCopyFeedback ()
    {
        CopyIcon.Visibility = Visibility.Collapsed;
        CheckIcon.Visibility = Visibility.Visible;
        ToolTipService.SetToolTip(CopyBtn , "Copied!");

        await Task.Delay(2000);

        CopyIcon.Visibility = Visibility.Visible;
        CheckIcon.Visibility = Visibility.Collapsed;
        ToolTipService.SetToolTip(CopyBtn , "Copy code");
    }

    private async void NewMDFile_Click (object? sender , RoutedEventArgs? e)
    {
        MdFileNameBox.Text = string.Empty;
        MdFileNameBox.Focus(FocusState.Programmatic);

        var result = await NewMdDialog.ShowAsync();

        if ( result is ContentDialogResult.Primary )
        {
            var fileName = string.IsNullOrWhiteSpace(MdFileNameBox.Text.Trim()) ? "README" : MdFileNameBox.Text.Trim();
            var (nameWithoutExt, extension) = ParseFileName(fileName , ".md");

            await CreateFileWithErrorHandling(nameWithoutExt , extension);
        }
    }

    private async void NewFile_Click (object sender , RoutedEventArgs e)
    {
        FileNameBox.Text = string.Empty;
        FileNameBox.Focus(FocusState.Programmatic);

        var result = await NewFileDialog.ShowAsync();

        if ( result is ContentDialogResult.Primary )
        {
            var fileName = string.IsNullOrWhiteSpace(FileNameBox.Text.Trim()) ? "Untitled" : FileNameBox.Text.Trim();
            var (nameWithoutExt, extension) = ParseFileName(fileName , string.Empty);

            await CreateFileWithErrorHandling(nameWithoutExt , extension);
        }
    }

    private static (string name, string extension) ParseFileName (string fileName , string defaultExtension)
    {
        var extension = Path.GetExtension(fileName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);

        if ( string.IsNullOrEmpty(extension) )
        {
            nameWithoutExt = fileName;
            extension = defaultExtension;
        }

        return (nameWithoutExt, extension);
    }

    private async Task CreateFileWithErrorHandling (string fileName , string extension)
    {
        var success = await ViewModel.CreateFileAsync(fileName , extension);
        if ( !success )
        {
            await _dialogService.ShowErrorAsync("File was not created. Please check the file name and destination folder.");
        }
    }

    private async Task InitializeWebViewAsync ()
    {
        if ( previewWebView?.CoreWebView2 is null )
        {
            await previewWebView!.EnsureCoreWebView2Async();
        }
    }

    private void CleanupWebView ()
    {
        try
        {
            if ( previewWebView?.CoreWebView2 is not null )
            {
                previewWebView.NavigateToString("<html><body></body></html>");
            }
        }
        catch ( Exception ex )
        {
            throw new Exception($"Error load: {ex.Message}" , ex);
        }
    }

    private async void About_Click (object sender , RoutedEventArgs e)
    {
        await AboutDialog.ShowAsync();
    }

    private void DisplayMode_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( DisplayMode.SelectedIndex >= 0 )
        {
            ViewModeChanged?.Invoke(this , DisplayMode.SelectedIndex);
        }
    }

    // ==================== KEYBOARD ACCELERATOR HANDLERS ====================
    // File commands

    private void TryExecuteCommand (System.Windows.Input.ICommand? command , KeyboardAcceleratorInvokedEventArgs args)
    {
        if ( command?.CanExecute(null) == true )
        {
            command.Execute(null);
        }
        args.Handled = true;
    }

    private void NewMDFileAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        NewMDFile_Click(null , null);
        args.Handled = true;
    }

    private void OpenFileAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args) => TryExecuteCommand(ViewModel.OpenFileCommand , args);

    private void OpenFolderAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args) => TryExecuteCommand(ViewModel.OpenFolderCommand , args);

    private void SaveAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args) => TryExecuteCommand(ViewModel.SaveCommand , args);

    private void SaveAsAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args) => TryExecuteCommand(ViewModel.SaveAsCommand , args);

    // Edit commands
    private void UndoAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        // if (ViewModel.UndoCommand?.CanExecute(null) == true)
        // {
        //     ViewModel.UndoCommand.Execute(null);
        // }
        args.Handled = true;
    }

    private void RedoAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        // TODO: Implement redo logic
        args.Handled = true;
    }

    private void CutAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        // TODO: Implement cut logic
        args.Handled = true;
    }

    private void CopyAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        // TODO: Implement copy logic
        args.Handled = true;
    }

    private void PasteAccelerator_Invoked (KeyboardAccelerator sender , KeyboardAcceleratorInvokedEventArgs args)
    {
        // TODO: Implement paste logic
        args.Handled = true;
    }

    public void Dispose ()
    {
        try
        {
            CleanupWebView();
            ViewModel.Cleanup();
        }
        catch ( Exception ex )
        {
            throw new Exception($"Error during cleanup in Dispose: {ex.Message}" , ex);
        }
    }
}