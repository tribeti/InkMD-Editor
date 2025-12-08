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
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
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
    <style>
        body {{
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Noto Sans', Helvetica, Arial, sans-serif;
            font-size: 16px;
            line-height: 1.6;
            color: #24292f;
            background-color: #ffffff;
            padding: 20px;
            max-width: 100%;
            margin: 0;
            box-sizing: border-box;
        }}

        h1, h2, h3, h4, h5, h6 {{
            margin-top: 24px;
            margin-bottom: 16px;
            font-weight: 600;
            line-height: 1.25;
        }}

        h1, h2 {{
            border-bottom: 1px solid #d0d7de;
            padding-bottom: 0.3em;
        }}

        h1 {{ font-size: 2em; }}
        h2 {{ font-size: 1.5em; }}
        h3 {{ font-size: 1.25em; }}

        p {{ margin-top: 0; margin-bottom: 16px; }}

        a {{
            color: #0969da;
            text-decoration: none;
        }}
        a:hover {{ text-decoration: underline; }}

        code {{
            background-color: rgba(175,184,193,0.2);
            border-radius: 6px;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 85%;
            padding: 0.2em 0.4em;
        }}

        pre {{
            background-color: #f6f8fa;
            border-radius: 6px;
            font-family: 'Consolas', 'Courier New', monospace;
            font-size: 85%;
            line-height: 1.45;
            overflow: auto;
            padding: 16px;
        }}

        pre code {{
            background-color: transparent;
            border: 0;
            padding: 0;
        }}

        blockquote {{
            border-left: 0.25em solid #d0d7de;
            color: #57606a;
            margin: 0 0 16px 0;
            padding: 0 1em;
        }}

        ul, ol {{
            margin-bottom: 16px;
            padding-left: 2em;
        }}

        li + li {{ margin-top: 0.25em; }}

        table {{
            border-collapse: collapse;
            border-spacing: 0;
            width: 100%;
            margin-bottom: 16px;
        }}

        table th {{
            font-weight: 600;
            background-color: #f6f8fa;
        }}

        table th, table td {{
            border: 1px solid #d0d7de;
            padding: 6px 13px;
        }}

        table tr {{
            background-color: #ffffff;
            border-top: 1px solid #d0d7de;
        }}

        table tr:nth-child(2n) {{
            background-color: #f6f8fa;
        }}

        img {{
            max-width: 100%;
            box-sizing: border-box;
        }}

        hr {{
            height: 0.25em;
            padding: 0;
            margin: 24px 0;
            background-color: #d0d7de;
            border: 0;
        }}

        input[type='checkbox'] {{
            margin-right: 0.5em;
        }}

        @media (prefers-color-scheme: dark) {{
            body {{
                color: #c9d1d9;
                background-color: #0d1117;
            }}

            h1, h2 {{
                border-bottom-color: #21262d;
            }}

            code {{
                background-color: rgba(110,118,129,0.4);
            }}

            pre {{
                background-color: #161b22;
            }}

            blockquote {{
                border-left-color: #3b434b;
                color: #8b949e;
            }}

            table th {{
                background-color: #161b22;
            }}

            table th, table td {{
                border-color: #3b434b;
            }}

            table tr {{
                background-color: #0d1117;
                border-top-color: #21262d;
            }}

            table tr:nth-child(2n) {{
                background-color: #161b22;
            }}

            a {{ color: #58a6ff; }}

            hr {{ background-color: #21262d; }}
        }}
    </style>
</head>
<body>
    {htmlBody}
</body>
</html>";
    }
}