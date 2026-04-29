using InkMD.Core.Messages;
using InkMD.Core.Services;
using InkMD_Editor.Services;
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

    // Guard flags: SetVirtualHostNameToFolderMapping must only be called once per WebView.
    // Calling it again on the same host name is a no-op but wastes time.
    private bool _splitHostMapped = false;
    private bool _previewHostMapped = false;

    // TaskCompletionSources used to await NavigationCompleted events without polling
    private TaskCompletionSource<bool>? _splitNavTcs = null;
    private TaskCompletionSource<bool>? _previewNavTcs = null;

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

    /// <summary>
    /// Initializes only the WebView2 that corresponds to the current view mode.
    /// Avoids creating two renderer processes when only one WebView is ever visible.
    /// </summary>
    private async Task InitializeWebViewsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (MilkdownPreview_Split is not null && !_splitPreviewReady
                && ViewModel.Tag == "split")
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureWebViewReadyAsync(MilkdownPreview_Split, cancellationToken);
            }

            if (MilkdownPreview is not null && !_previewReady
                && ViewModel.Tag == "preview")
            {
                cancellationToken.ThrowIfCancellationRequested();
                await EnsureWebViewReadyAsync(MilkdownPreview, cancellationToken);
            }
        }
        catch (OperationCanceledException) { /* expected on mode switch */ }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] WebView init error: {ex.Message}");
        }
    }

    // ─── Load Milkdown ──────────────────────────────────
    private async Task EnsureWebViewReadyAsync(WebView2 webView, CancellationToken cancellationToken = default)
    {
        var envService = (App.Current as App)?.Services
            .GetService(typeof(WebView2EnvironmentService)) as WebView2EnvironmentService;

        var sharedEnv = envService?.Environment;

        if (sharedEnv is not null)
            await webView.EnsureCoreWebView2Async(sharedEnv);
        else
            await webView.EnsureCoreWebView2Async();

        cancellationToken.ThrowIfCancellationRequested();

        webView.WebMessageReceived -= WebView_WebMessageReceived;
        webView.WebMessageReceived += WebView_WebMessageReceived;

        await webView.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(
            """document.addEventListener('keydown',function(e){if (e.ctrlKey && (e.key === 's' || e.key === 'S')){ e.preventDefault();window.chrome?.webview?.postMessage({ type: 'saveRequest',saveAs: e.shiftKey});}},true);""");

        bool isSplit = ReferenceEquals(webView, MilkdownPreview_Split);
        if (isSplit && !_splitHostMapped)
        {
            MapVirtualHost(webView);
            _splitHostMapped = true;
        }
        else if (!isSplit && !_previewHostMapped)
        {
            MapVirtualHost(webView);
            _previewHostMapped = true;
        }

        webView.CoreWebView2.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (isSplit)
            _splitNavTcs = tcs;
        else
            _previewNavTcs = tcs;

        void OnNavigationCompleted(CoreWebView2 sender, CoreWebView2NavigationCompletedEventArgs args)
        {
            sender.NavigationCompleted -= OnNavigationCompleted;
            tcs.TrySetResult(args.IsSuccess);
        }

        webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

        // Navigate to the Milkdown page
        webView.Source = new Uri("https://editor.local/index.html");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(10));

        try
        {
            await tcs.Task.WaitAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
            cancellationToken.ThrowIfCancellationRequested();
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] Navigation timed out for {(isSplit ? "split" : "preview")} WebView.");
        }
    }

    private void MapVirtualHost(WebView2 webView)
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
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] SyncTheme error: {ex.Message}");
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
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] RenderInMilkdown error: {ex.Message}");
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
            if (!root.TryGetProperty("type", out var typeProp))
                return;

            var messageType = typeProp.GetString();

            if (messageType == "saveRequest")
            {
                bool saveAs = root.TryGetProperty("saveAs", out var saProp) && saProp.GetBoolean();
                DispatcherQueue.TryEnqueue(() =>
                    RxMessageBus.Default.Publish(new SaveFileMessage(IsNewFile: saveAs)));
                return;
            }

            if (messageType == "ready")
            {
                bool isSplit = ReferenceEquals(sender, MilkdownPreview_Split);
                DispatcherQueue.TryEnqueue(async () =>
                {
                    if (isSplit)
                        _splitPreviewReady = true;
                    else
                        _previewReady = true;

                    System.Diagnostics.Debug.WriteLine($"[TabViewContent] Editor bridge ready ({(isSplit ? "split" : "preview")})");

                    await SyncTheme(sender);

                    if (_pendingPreviewContent is not null && IsPreviewReady)
                    {
                        var pending = _pendingPreviewContent;
                        _pendingPreviewContent = null;
                        await RenderInMilkdown(pending);
                    }
                });
                return;
            }

            // ── Content changed ──────────────────────────────────────────────────
            if (messageType == "contentChanged")
            {
                if (root.TryGetProperty("content", out var contentProp))
                {
                    var newContent = contentProp.GetString();

                    if (newContent != ViewModel.CurrentContent)
                    {
                        ViewModel.CurrentContent = newContent;
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            _isUpdatingFromWebView = true;
                            try
                            {
                                SetContentToCurrentEditBox(newContent ?? string.Empty);
                            }
                            finally
                            {
                                _isUpdatingFromWebView = false;
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] WebMessageReceived error: {ex.Message}");
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

    public IEnumerable<string> GetContentToSaveFile()
    {
        if (CurrentEditBox is not null)
            return CurrentEditBox.Lines;

        return (ViewModel.CurrentContent ?? string.Empty)
           .Split(["\r\n", "\n"], StringSplitOptions.None);
    }

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

        // Suspend the WebView that is being hidden to reduce memory usage
        SuspendInactiveWebViews(tag);

        ViewModel.Tag = tag;
        SetContentToCurrentEditBox(ViewModel.CurrentContent ?? string.Empty);

        if (tag is "split" or "preview")
        {
            if (_viewModeCts is not null)
            {
                _viewModeCts.Cancel();
                _viewModeCts.Dispose();
            }

            _viewModeCts = new CancellationTokenSource();
            var token = _viewModeCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(150, token);
                    DispatcherQueue.TryEnqueue(async () =>
                    {
                        if (token.IsCancellationRequested)
                            return;

                        await InitializeWebViewsAsync(token);
                        RenderPreviewIfReady(ViewModel.CurrentContent ?? string.Empty);
                    });
                }
                catch (OperationCanceledException) { }
            });
        }
    }

    /// <summary>
    /// Sets inactive WebViews to Low memory level and restores Normal for the active one.
    /// See: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/performance#use-memory-management-apis
    /// </summary>
    private void SuspendInactiveWebViews(string newTag)
    {
        try
        {
            // Suspend the split WebView when switching away from it
            if (newTag != "split" && MilkdownPreview_Split?.CoreWebView2 is { } splitCore && _splitPreviewReady)
                splitCore.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;

            // Suspend the preview WebView when switching away from it
            if (newTag != "preview" && MilkdownPreview?.CoreWebView2 is { } previewCore && _previewReady)
                previewCore.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Low;

            // Restore Normal for the one that's becoming active
            if (newTag == "split" && MilkdownPreview_Split?.CoreWebView2 is { } activeSplitCore && _splitPreviewReady)
                activeSplitCore.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;

            if (newTag == "preview" && MilkdownPreview?.CoreWebView2 is { } activePreviewCore && _previewReady)
                activePreviewCore.MemoryUsageTargetLevel = CoreWebView2MemoryUsageTargetLevel.Normal;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[TabViewContent] SuspendInactiveWebViews error: {ex.Message}");
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
        _splitHostMapped = false;
        _previewHostMapped = false;
        _pendingPreviewContent = null;
        _splitNavTcs = null;
        _previewNavTcs = null;
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