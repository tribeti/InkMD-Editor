using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;
using InkMD_Editor.Services;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Threading.Tasks;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl
{
    private readonly FileService _fileService = new();
    private readonly DialogService _dialogService = new();
    private readonly MarkdownPipeline _markdownPipeline;

    public MainMenu ()
    {
        InitializeComponent();

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();
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
    private static void Save ()
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
            var templates = await TemplateService.GetAllTemplatesAsync();
            TemplateGridView.ItemsSource = templates;
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Không thể load templates: {ex.Message}");
        }
    }

    private async void TemplateGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( e.AddedItems.Count > 0 && e.AddedItems [0] is TemplateInfo selectedTemplate )
        {
            try
            {
                var content = await TemplateService.LoadTemplateAsync(selectedTemplate.FileName);
                TemplateFlyout.Hide();
                TemplateGridView.SelectedItem = null;
                await ShowTemplatePreviewDialog(selectedTemplate.DisplayName , content);
            }
            catch ( Exception ex )
            {
                await _dialogService.ShowErrorAsync($"Không thể load template: {ex.Message}");
            }
        }
    }

    private async Task ShowTemplatePreviewDialog (string templateName , string content)
    {
        if ( TemplateDialog is null || previewWebView is null )
        {
            await _dialogService.ShowErrorAsync("Lỗi giao diện: Không tìm thấy Dialog hoặc WebView.");
            return;
        }

        TemplateDialog.Title = $"Template Preview: {templateName}";
        TemplateDialog.XamlRoot = this.XamlRoot;
        TemplateDialog.DefaultButton = ContentDialogButton.Primary;

        try
        {
            if ( previewWebView.CoreWebView2 == null )
            {
                await previewWebView.EnsureCoreWebView2Async();
            }
            string html = ConvertMarkdownToHtml(content);
            previewWebView.NavigateToString(html);
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Không thể hiển thị preview: {ex.Message}");
            return;
        }

        var result = await TemplateDialog.ShowAsync();
        if ( result is ContentDialogResult.Primary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: true));
        }
        else if ( result is ContentDialogResult.Secondary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: false));
        }

        TemplateDialog.Closed += (s , e) => {
            if ( previewWebView.CoreWebView2 is not null )
                previewWebView.NavigateToString("");
        };
    }

    private string ConvertMarkdownToHtml (string markdown)
    {
        if ( string.IsNullOrWhiteSpace(markdown) )
            return GetEmptyPreviewHtml();

        string htmlBody = Markdown.ToHtml(markdown , _markdownPipeline);
        return WrapWithGitHubStyle(htmlBody);
    }

    private static string GetEmptyPreviewHtml ()
    {
        return WrapWithGitHubStyle("<p style='color:#888; text-align:center; margin-top:50px;'>Preview sẽ hiển thị ở đây...</p>");
    }

    private static string WrapWithGitHubStyle (string htmlBody)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <link rel=""stylesheet"" href=""https://cdn.jsdelivr.net/gh/tribeti/Java@master/style.css"">
    <style>
        body {{
            padding: 20px;
            margin: 0;
            overflow-y: auto;
        }}
    </style>
</head>
<body>
    {htmlBody}
</body>
</html>";
    }
}