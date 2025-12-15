using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;
using InkMD_Editor.Models;
using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl, IDisposable
{
    private readonly DialogService _dialogService = new();
    private readonly MarkdownPipeline _markdownPipeline;
    private MainMenuViewModel ViewModel { get; set; } = new();
    public ObservableCollection<IconItem> IconItems { get; set; } = [];
    private bool _iconsLoaded = false;
    private ObservableCollection<MdTemplate>? _templateCache;
    private List<string> _selectedIconsList = [];

    public MainMenu ()
    {
        InitializeComponent();
        this.DataContext = ViewModel;
        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();
        this.Unloaded += (s , e) => Dispose();
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
            if ( _templateCache is not null )
            {
                TemplateGridView.ItemsSource = _templateCache;
                return;
            }

            var templates = await TemplateService.GetAllTemplatesAsync();
            _templateCache = new ObservableCollection<MdTemplate>(templates);
            TemplateGridView.ItemsSource = _templateCache;
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Không thể load templates: {ex.Message}");
        }
    }

    private async void TemplateGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        if ( e.AddedItems.Count > 0 && e.AddedItems [0] is MdTemplate selectedTemplate )
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

    private async void AppBarButton_Click (object sender , RoutedEventArgs e)
    {
        _selectedIconsList.Clear();
        await LoadIconsAsync();
        IconsDialog.XamlRoot = this.XamlRoot;
        IconsDialog.DefaultButton = ContentDialogButton.Primary;
        await IconsDialog.ShowAsync();
    }

    private string GenerateIconsCode ()
    {
        if ( _selectedIconsList.Count == 0 )
        {
            return "![](https://ink-md-server.vercel.app/api?i=)";
        }

        string iconsList = string.Join("," , _selectedIconsList);
        string skillIconUrl = $"![](https://ink-md-server.vercel.app/api?i={iconsList})";

        return skillIconUrl;
    }

    private async Task LoadIconsAsync ()
    {
        if ( _iconsLoaded && IconItems.Count > 0 )
            return;

        try
        {
            var icons = await TemplateService.GetAllIconsAsync();
            IconItems.Clear();
            foreach ( var icon in icons )
            {
                IconItems.Add(icon);
            }
            _iconsLoaded = true;
        }
        catch ( Exception ex )
        {
            await _dialogService.ShowErrorAsync($"Không thể load icons: {ex.Message}");
        }
    }

    private void IconGridView_SelectionChanged (object sender , SelectionChangedEventArgs e)
    {
        foreach ( var item in e.AddedItems )
        {
            if ( item is IconItem icon )
            {
                if ( !_selectedIconsList.Contains(icon.Name) )
                {
                    _selectedIconsList.Add(icon.Name);
                }
            }
        }

        foreach ( var item in e.RemovedItems )
        {
            if ( item is IconItem icon )
            {
                _selectedIconsList.Remove(icon.Name);
            }
        }

        // Cập nhật code hiển thị
        string generatedCode = GenerateIconsCode();
        CodeDisplay.Text = generatedCode;
    }

    private async void CopyBtn_Click (object sender , RoutedEventArgs e)
    {
        string contentToCopy = CodeDisplay.Text;

        if ( string.IsNullOrEmpty(contentToCopy) )
            return;

        DataPackage dataPackage = new DataPackage();
        dataPackage.SetText(contentToCopy);
        Clipboard.SetContent(dataPackage);

        CopyIcon.Visibility = Visibility.Collapsed;
        CheckIcon.Visibility = Visibility.Visible;
        ToolTipService.SetToolTip(CopyBtn , "Copied!");

        await Task.Delay(2000);

        CopyIcon.Visibility = Visibility.Visible;
        CheckIcon.Visibility = Visibility.Collapsed;
        ToolTipService.SetToolTip(CopyBtn , "Copy code");
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
            if ( previewWebView.CoreWebView2 is null )
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
        CleanupWebView();

        if ( result is ContentDialogResult.Primary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: true));
        }
        else if ( result is ContentDialogResult.Secondary )
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: false));
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
            System.Diagnostics.Debug.WriteLine($"WebView cleanup error: {ex.Message}");
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

    private async void NewMDFile_Click (object sender , RoutedEventArgs e)
    {
        MdFileNameBox.Text = string.Empty;
        MdFileNameBox.Focus(FocusState.Programmatic);

        var result = await NewMdDialog.ShowAsync();

        if ( result == ContentDialogResult.Primary )
        {
            //CreateMdFile();
        }
    }

    private async void NewFile_Click (object sender , RoutedEventArgs e)
    {
        MdFileNameBox.Text = string.Empty;
        MdFileNameBox.Focus(FocusState.Programmatic);

        var result = await NewFileDialog.ShowAsync();

        if ( result == ContentDialogResult.Primary )
        {
            //CreateFile();
        }
    }

    private async void About_Click(object sender, RoutedEventArgs e)
    {
        await AboutDialog.ShowAsync();
    }

    public void Dispose ()
    {
        try
        {
            CleanupWebView();
            _templateCache?.Clear();
            _templateCache = null;
            IconItems.Clear();
        }
        catch ( Exception ex )
        {
            System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
        }
    }
}