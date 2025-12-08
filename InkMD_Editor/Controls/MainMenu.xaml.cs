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
    private string? _pendingTemplateContent;

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
                _pendingTemplateContent = content;

                TemplateFlyout.Hide();
                TemplateGridView.SelectedItem = null;
                await ShowTemplatePreviewDialog(selectedTemplate.DisplayName , content);
            }
            catch ( Exception ex )
            {
                await _dialogService.ShowErrorAsync($"Không thể load templates: {ex.Message}");
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
            Padding = new Thickness(12) ,
            MaxHeight = 300
        };

        var scrollViewer = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var previewTextBlock = new TextBlock
        {
            Text = content ,
            TextWrapping = TextWrapping.Wrap ,
            FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Cascadia Mono") ,
            IsTextSelectionEnabled = true
        };

        scrollViewer.Content = previewTextBlock;
        previewBorder.Child = scrollViewer;
        stackPanel.Children.Add(previewBorder);

        // Buttons section
        var buttonPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal ,
            HorizontalAlignment = HorizontalAlignment.Right ,
            Spacing = 8
        };

        var addButton = new Button
        {
            Content = "Add (New File)" ,
            Style = (Style) Application.Current.Resources ["AccentButtonStyle"]
        };

        var insertButton = new Button
        {
            Content = "Insert (Current Doc)"
        };

        // Event handlers
        addButton.Click += (s , e) =>
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: true));
            dialog.Hide();
        };

        insertButton.Click += (s , e) =>
        {
            WeakReferenceMessenger.Default.Send(new TemplateSelectedMessage(content , createNewFile: false));
            dialog.Hide();
        };

        buttonPanel.Children.Add(insertButton);
        buttonPanel.Children.Add(addButton);

        stackPanel.Children.Add(buttonPanel);
        dialog.Content = stackPanel;

        await dialog.ShowAsync();
    }
}
