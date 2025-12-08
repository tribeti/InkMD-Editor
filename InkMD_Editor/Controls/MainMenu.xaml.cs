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
        var dialog = new ContentDialog
        {
            Title = $"Template Preview: {templateName}" ,
            CloseButtonText = "Cancel" ,
            DefaultButton = ContentDialogButton.Primary ,
            PrimaryButtonText = "Add (New File)" ,
            SecondaryButtonText = "Insert (Current Doc)" ,
            XamlRoot = this.XamlRoot
        };

        var stackPanel = new StackPanel
        {
            Spacing = 16
        };

        var previewBorder = new Border
        {
            Background = (Microsoft.UI.Xaml.Media.Brush) Application.Current.Resources ["CardBackgroundFillColorDefaultBrush"] ,
            BorderBrush = (Microsoft.UI.Xaml.Media.Brush) Application.Current.Resources ["CardStrokeColorDefaultBrush"] ,
            BorderThickness = new Thickness(1) ,
            CornerRadius = new CornerRadius(4) ,
            Height = 400 ,
            Width = 600
        };

        var previewWebView = new WebView2();

        try
        {
            await previewWebView.EnsureCoreWebView2Async();
            string html = ConvertMarkdownToHtml(content);
            previewWebView.NavigateToString(html);
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Không thể hiển thị preview: {ex.Message}");
            return;
        }

        previewBorder.Child = previewWebView;
        stackPanel.Children.Add(previewBorder);
        dialog.Content = stackPanel;

        dialog.Closed += (s , e) =>
        {
            if ( previewBorder.Child == previewWebView )
            {
                previewBorder.Child = null;
            }

            previewWebView?.Close();
            previewWebView = null;
        };

        var result = await dialog.ShowAsync();

        if ( result is ContentDialogResult.Primary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: true));
        }
        else if ( result is ContentDialogResult.Secondary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: false));
        }
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
</head>
<body>
    {htmlBody}
</body>
</html>";
    }
}