using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Messagers;
using InkMD_Editor.Models;
using InkMD_Editor.Services;
using Markdig;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl, IDisposable
{
    private readonly FileService _fileService = new();
    private readonly DialogService _dialogService = new();
    private readonly MarkdownPipeline _markdownPipeline;
    public ObservableCollection<SkillItem> Skills { get; set; } = new();

    private bool _skillsLoaded = false;
    private ObservableCollection<TemplateInfo>? _templateCache;

    public MainMenu ()
    {
        InitializeComponent();

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .Build();

        LoadSkillsData();
        this.Unloaded += (s , e) => Dispose();
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
            if ( _templateCache is not null )
            {
                TemplateGridView.ItemsSource = _templateCache;
                return;
            }

            var templates = await TemplateService.GetAllTemplatesAsync();
            _templateCache = new ObservableCollection<TemplateInfo>(templates);
            TemplateGridView.ItemsSource = _templateCache;
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

    private async void AppBarButton_Click (object sender , RoutedEventArgs e)
    {
        IconsDialog.XamlRoot = this.XamlRoot;
        IconsDialog.DefaultButton = ContentDialogButton.Primary;
        await IconsDialog.ShowAsync();
    }

    private void LoadSkillsData ()
    {
        if ( _skillsLoaded )
            return;

        string rawData = "ableton,activitypub,actix,adonis,ae,aiscript,alpinejs,anaconda,androidstudio,angular,ansible,apollo,apple,appwrite,arch,arduino,astro,atom,au,autocad,aws,azul,azure,babel,bash,bevy,bitbucket,blender,bootstrap,bsd,bun,c,cs,cpp,crystal,cassandra,clion,clojure,cloudflare,cmake,codepen,coffeescript,css,cypress";
        var items = rawData.Split(',');

        Skills.Clear();
        foreach ( var item in items )
        {
            Skills.Add(new SkillItem(item.Trim()));
        }

        _skillsLoaded = true;
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

    public void Dispose ()
    {
        try
        {
            CleanupWebView();
            _templateCache?.Clear();
            _templateCache = null;
            Skills.Clear();
        }
        catch ( Exception ex )
        {
            System.Diagnostics.Debug.WriteLine($"Dispose error: {ex.Message}");
        }
    }
}