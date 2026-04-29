using InkMD.Core.Messages;
using InkMD.Core.Services;
using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TextControlBoxNS;
using Windows.ApplicationModel;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl, IEditableContent
{
    public TabViewContentViewModel ViewModel { get; } = new();

    private bool _splitPreviewReady = false;
    private bool _previewReady = false;
    private string? _pendingPreviewContent = null;
    private bool _isUpdatingFromWebView = false;
    private CancellationTokenSource? _viewModeCts = null;
    private IDisposable? _themeSubscription = null;

    private WebView2? CurrentPreviewView => ViewModel.Tag switch
    {
        "split" => MilkdownPreview_Split,
        "preview" => MilkdownPreview,
        _ => null
    };

    private bool IsPreviewReady => ViewModel.Tag switch
    {
        "split" => _splitPreviewReady,
        "preview" => _previewReady,
        _ => false
    };

    private TextControlBox? CurrentEditBox => ViewModel.Tag switch
    {
        "md" => EditBox,
        "split" => EditBox_Split,
        _ => null
    };

    public TabViewContent()
    {
        InitializeComponent();
        DataContext = ViewModel;
        _themeSubscription = RxMessageBus.Default
            .Subscribe<ThemeChangedMessage>()
            .Subscribe(msg =>
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (MilkdownPreview_Split is not null && _splitPreviewReady)
                        await SyncTheme(MilkdownPreview_Split, msg.Theme);

                    if (MilkdownPreview is not null && _previewReady)
                        await SyncTheme(MilkdownPreview, msg.Theme);
                });
            });

        Loaded += TabViewContent_Loaded;
    }

    private async void TabViewContent_Loaded(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        InitializeEditBoxes();
        await InitializeWebViewsAsync();

        if (!string.IsNullOrEmpty(ViewModel.CurrentContent))
        {
            SetContentToCurrentEditBox(ViewModel.CurrentContent);
            RenderPreviewIfReady(ViewModel.CurrentContent);
        }
    }

    // ─── Init ────────────────────────────────────────────────────────

    private void InitializeEditBoxes()
    {
        if (EditBox is not null)
        {
            EditBox.EnableSyntaxHighlighting = true;
            EditBox.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
            EditBox.DoAutoPairing = true;
            EditBox.SelectionChanged += (s, e) => UpdateFormattingState(EditBox);
        }

        if (EditBox_Split is not null)
        {
            EditBox_Split.EnableSyntaxHighlighting = true;
            EditBox_Split.SelectSyntaxHighlightingById(SyntaxHighlightID.Markdown);
            EditBox_Split.DoAutoPairing = true;
            EditBox_Split.SelectionChanged += (s, e) => UpdateFormattingState(EditBox_Split);
        }
    }

    private async Task InitializeWebViewsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (MilkdownPreview_Split is not null && !_splitPreviewReady)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await MilkdownPreview_Split.EnsureCoreWebView2Async();
                MilkdownPreview_Split.WebMessageReceived += WebView_WebMessageReceived;
                await LoadMilkdownIntoWebView(MilkdownPreview_Split, cancellationToken);
                _splitPreviewReady = true;
            }

            if (MilkdownPreview is not null && !_previewReady)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await MilkdownPreview.EnsureCoreWebView2Async();
                MilkdownPreview.WebMessageReceived += WebView_WebMessageReceived;
                await LoadMilkdownIntoWebView(MilkdownPreview, cancellationToken);
                _previewReady = true;
            }

            if (_pendingPreviewContent is not null && IsPreviewReady)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await RenderInMilkdown(_pendingPreviewContent);
                _pendingPreviewContent = null;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebView init error: {ex.Message}");
        }
    }

    // ─── Load Milkdown vào WebView2 ──────────────────────────────────

    private async Task LoadMilkdownIntoWebView(WebView2 webView, CancellationToken cancellationToken = default)
    {
        var distPath = Path.Combine(
            Package.Current.InstalledLocation.Path,
            "dist"
        );

        webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            "editor.local",
            distPath,
            CoreWebView2HostResourceAccessKind.Allow
        );

        webView.Source = new Uri("https://editor.local/index.html");

        for (int i = 0; i < 30; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await Task.Delay(100, cancellationToken);
            try
            {
                var result = await webView.CoreWebView2.ExecuteScriptAsync(
                    "typeof window.editorBridge !== 'undefined' && window.editorBridge.isReady ? 'ready' : 'not_ready'"
                );
                if (result == "\"ready\"")
                {
                    await SyncTheme(webView);
                    return;
                }
            }
            catch { }
        }
    }

    private async Task SyncTheme(WebView2 webView, string? theme = null)
    {
        try
        {
            theme ??= ActualTheme == Microsoft.UI.Xaml.ElementTheme.Dark ? "dark" : "light";

            await webView.CoreWebView2.ExecuteScriptAsync(
                $"window.editorBridge?.setTheme('{theme}')"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SyncTheme error: {ex.Message}");
        }
    }

    // ─── Render markdown vào Milkdown preview ────────────────────────

    private async Task RenderInMilkdown(string markdown)
    {
        if (CurrentPreviewView is null || !IsPreviewReady)
        {
            _pendingPreviewContent = markdown;
            return;
        }

        try
        {
            var escaped = JsonSerializer.Serialize(markdown);
            await CurrentPreviewView.CoreWebView2.ExecuteScriptAsync($"window.editorBridge?.setContent({escaped})"
            );
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RenderInMilkdown error: {ex.Message}");
        }
    }

    private void WebView_WebMessageReceived(WebView2 sender, CoreWebView2WebMessageReceivedEventArgs args)
    {
        try
        {
            var json = args.WebMessageAsJson;
            if (string.IsNullOrEmpty(json))
                return;

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.TryGetProperty("type", out var typeProp) && typeProp.GetString() == "contentChanged")
            {
                if (root.TryGetProperty("content", out var contentProp))
                {
                    var newContent = contentProp.GetString();

                    if (newContent != ViewModel.CurrentContent)
                    {
                        _isUpdatingFromWebView = true;
                        try
                        {
                            ViewModel.CurrentContent = newContent;
                            SetContentToCurrentEditBox(newContent ?? string.Empty);
                        }
                        finally
                        {
                            _isUpdatingFromWebView = false;
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"WebMessageReceived error: {ex.Message}");
        }
    }

    // ─── TextControlBox handlers ─────────────────────────────────────

    private void EditBox_TextChanged(TextControlBox sender)
    {
        if (_isUpdatingFromWebView)
            return;

        var text = sender.GetText();
        ViewModel.CurrentContent = text;
        UpdateFormattingState(sender);

        // Cập nhật Milkdown preview nếu split mode
        if (ViewModel.Tag == "split" && _splitPreviewReady)
        {
            _ = RenderInMilkdown(text);
        }
    }

    // ─── IEditableContent ────────────────────────────────────────────

    public void SetContent(string text, string? fileName)
    {
        ViewModel.IsLoadingContent = true;
        ViewModel.FileName = fileName;
        ViewModel.SetOriginalContent(text);
        SetContentToCurrentEditBox(text);

        _pendingPreviewContent = text;
        RenderPreviewIfReady(text);

        ViewModel.IsLoadingContent = false;
    }

    private void SetContentToCurrentEditBox(string text) => CurrentEditBox?.LoadText(text);

    private void RenderPreviewIfReady(string content)
    {
        if (ViewModel.Tag is not ("split" or "preview"))
            return;

        if (IsPreviewReady)
        {
            _pendingPreviewContent = null;
            _ = RenderInMilkdown(content);
        }
    }

    public string GetContent() => CurrentEditBox?.GetText() ?? ViewModel.CurrentContent ?? string.Empty;

    public IEnumerable<string> GetContentToSaveFile() => CurrentEditBox?.Lines ?? [];

    public string GetFilePath() => ViewModel.FilePath ?? string.Empty;
    public string GetFileName() => ViewModel.FileName ?? string.Empty;
    public void SetFilePath(string filePath, string fileName) => ViewModel.SetFilePath(filePath, fileName);
    public bool IsDirty() => ViewModel.IsDirty;
    public void MarkAsClean() => ViewModel.MarkAsClean();
    public void Undo() => CurrentEditBox?.Undo();
    public void Redo() => CurrentEditBox?.Redo();
    public void Cut() => CurrentEditBox?.Cut();
    public void Copy() => CurrentEditBox?.Copy();
    public void Paste() => CurrentEditBox?.Paste();
    public void ApplyBold() => ToggleFormattingStyle("**");
    public void ApplyItalic() => ToggleFormattingStyle("*");
    public void ApplyStrikethrough() => ToggleStrikethrough();

    public void InsertText(string text)
    {
        if (CurrentEditBox is null)
            return;
        CurrentEditBox.AddLine(CurrentEditBox.CurrentLineIndex, text);
    }

    // ─── View mode ───────────────────────────────────────────────────

    public void SetViewMode(string tag)
    {
        var currentText = GetContent();
        if (!string.IsNullOrEmpty(currentText))
            ViewModel.CurrentContent = currentText;

        ViewModel.Tag = tag;
        SetContentToCurrentEditBox(ViewModel.CurrentContent ?? string.Empty);

        if (tag is "split" or "preview")
        {
            // Cancel previous pending view mode initialization task
            if (_viewModeCts is not null)
            {
                _viewModeCts.Cancel();
                _viewModeCts.Dispose();
            }

            _viewModeCts = new CancellationTokenSource();
            var token = _viewModeCts.Token;

            _ = Task.Run(async () =>
            {
                await Task.Delay(150, token);
                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (token.IsCancellationRequested)
                        return;

                    await InitializeWebViewsAsync(token);
                    RenderPreviewIfReady(ViewModel.CurrentContent ?? string.Empty);
                });
            });
        }
    }

    // ─── Formatting ──────────────────────────────────────────────────

    private void ToggleFormattingStyle(string marker)
    {
        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;

        bool hasStrike = IsFormattedWith(text, "~~");
        string coreText = hasStrike ? text[2..^2] : text;
        string newText = IsWrappedWith(coreText, marker)
            ? coreText.Substring(marker.Length, coreText.Length - marker.Length * 2)
            : $"{marker}{coreText}{marker}";

        if (hasStrike)
            newText = $"~~{newText}~~";
        ApplyTextChange(newText);
    }

    private void ToggleStrikethrough()
    {
        string text = GetTextToFormat();
        if (string.IsNullOrEmpty(text))
            return;
        ApplyTextChange(IsFormattedWith(text, "~~") ? text[2..^2] : $"~~{text}~~");
    }

    private string GetTextToFormat()
    {
        if (CurrentEditBox is null)
            return string.Empty;
        if (CurrentEditBox.HasSelection)
            return CurrentEditBox.SelectedText ?? string.Empty;
        try
        {
            int line = CurrentEditBox.CurrentLineIndex;
            if (line < 0 || line >= CurrentEditBox.NumberOfLines)
                return string.Empty;
            return CurrentEditBox.GetLineText(line) ?? string.Empty;
        }
        catch { return string.Empty; }
    }

    private void ApplyTextChange(string newText)
    {
        if (CurrentEditBox is null)
            return;
        try
        {
            if (CurrentEditBox.HasSelection)
                CurrentEditBox.SelectedText = newText;
            else
                CurrentEditBox.SetLineText(CurrentEditBox.CurrentLineIndex, newText);
            UpdateFormattingState(CurrentEditBox);
        }
        catch { }
    }

    private bool IsWrappedWith(string text, string marker)
    {
        if (text.Length < marker.Length * 2)
            return false;
        if (marker == "*" && text.StartsWith("**") && text.EndsWith("**") && !text.StartsWith("***"))
            return false;
        return text.StartsWith(marker) && text.EndsWith(marker);
    }

    private bool IsFormattedWith(string text, string marker) =>
        !string.IsNullOrEmpty(text) && text.StartsWith(marker) && text.EndsWith(marker) && text.Length >= 2 * marker.Length;

    private void UpdateFormattingState(TextControlBox sender)
    {
        if (sender is null)
            return;
        string text = GetTextToFormat();
        string textWithoutStrike = text;
        bool hasStrike = false;

        if (text.StartsWith("~~") && text.EndsWith("~~") && text.Length > 4)
        {
            textWithoutStrike = text[2..^2];
            hasStrike = true;
        }

        bool hasBoldItalic = IsFormattedWith(textWithoutStrike, "***");
        bool hasBold = IsFormattedWith(textWithoutStrike, "**") || hasBoldItalic;
        bool hasItalic = (IsFormattedWith(textWithoutStrike, "*") && !IsFormattedWith(textWithoutStrike, "**")) || hasBoldItalic;

        ViewModel.IsBoldActive = hasBold;
        ViewModel.IsItalicActive = hasItalic;
        ViewModel.IsStrikethroughActive = hasStrike;

        RxMessageBus.Default.Publish(new FormattingStateMessage(hasBold, hasItalic, hasStrike));
    }

    // ─── Dispose ─────────────────────────────────────────────────────

    public void DisposeWebView()
    {
        if (MilkdownPreview_Split is not null)
        {
            MilkdownPreview_Split.WebMessageReceived -= WebView_WebMessageReceived;
            MilkdownPreview_Split.Close();
        }

        if (MilkdownPreview is not null)
        {
            MilkdownPreview.WebMessageReceived -= WebView_WebMessageReceived;
            MilkdownPreview.Close();
        }

        // Clear related flags and pending content
        _splitPreviewReady = false;
        _previewReady = false;
        _pendingPreviewContent = null;
    }

    public void Dispose()
    {
        // Cancel any pending view mode initialization task
        if (_viewModeCts is not null)
        {
            _viewModeCts.Cancel();
            _viewModeCts.Dispose();
            _viewModeCts = null;
        }

        _themeSubscription?.Dispose();
        _themeSubscription = null;

        ViewModel.Dispose();
        DisposeWebView();
    }
}