using Microsoft.Web.WebView2.Core;
using System;
using System.Threading.Tasks;

namespace InkMD.App.Services;

/// <summary>
/// Singleton service that pre-creates and caches a shared <see cref="CoreWebView2Environment"/>
/// for the entire application lifetime. Sharing one environment across all WebView2 controls
/// avoids spawning redundant browser processes per instance, reducing startup time and memory.
/// See: https://learn.microsoft.com/en-us/microsoft-edge/webview2/concepts/performance#share-webview2-environments
/// </summary>
public sealed class WebView2EnvironmentService
{
    private CoreWebView2Environment? _environment;
    private Task? _initTask;

    /// <summary>
    /// The shared environment. May be null if initialization failed or hasn't been called yet.
    /// </summary>
    public CoreWebView2Environment? Environment => _environment;

    /// <summary>
    /// Pre-warm the shared <see cref="CoreWebView2Environment"/>.
    /// Safe to call multiple times — initialization only happens once.
    /// </summary>
    public Task InitializeAsync()
    {
        // Ensure only one initialization ever runs
        _initTask ??= CreateEnvironmentAsync();
        return _initTask;
    }

    private async Task CreateEnvironmentAsync()
    {
        try
        {
            _environment = await CoreWebView2Environment.CreateAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[WebView2EnvironmentService] Failed to pre-create environment: {ex.Message}");
            // Fall back gracefully — WebView2 controls will create their own environment
            _environment = null;
        }
    }
}
