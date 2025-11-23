using InkMD_Editor.ViewModels;
using Markdig;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;

namespace InkMD_Editor.Controls;

public sealed partial class TabViewContent : UserControl
{
    public TabViewContentViewModel ViewModel { get; set; } = new();
    private readonly MarkdownPipeline _markdownPipeline;

    public TabViewContent ()
    {
        InitializeComponent();
        this.DataContext = ViewModel;

        _markdownPipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .Build();

        InitializeWebView();
    }

    private void MdEditor_TextChanged (object sender , RoutedEventArgs e)
    {
        var doc = MdEditor.Document;
        doc.GetText(TextGetOptions.None , out string text);
        ViewModel?.UpdateWordCount(text);
        UpdateMarkdownPreview(text);
    }

    public void SetContent (string text , string fileName)
    {
        var doc = MdEditor.Document;
        doc.SetText(TextSetOptions.None , text);
        ViewModel.FileName = fileName;
        ViewModel.UpdateWordCount(text);
        UpdateMarkdownPreview(text);
    }

    public string GetContent ()
    {
        try
        {
            if ( MdEditor is null )
            {
                Debug.WriteLine("MdEditor is null");
                return string.Empty;
            }

            var doc = MdEditor.Document;
            if ( doc is null )
            {
                Debug.WriteLine("Document is null");
                return string.Empty;
            }

            doc.GetText(TextGetOptions.None , out string text);
            Debug.WriteLine($"GetContent: Retrieved {text?.Length ?? 0} characters");
            return text ?? string.Empty;
        }
        catch ( Exception ex )
        {
            Debug.WriteLine($"GetContent error: {ex.Message}");
            return string.Empty;
        }
    }

    private async void InitializeWebView ()
    {
        try
        {
            await MarkdownPreview.EnsureCoreWebView2Async();
            MarkdownPreview.NavigateToString(GetEmptyPreviewHtml());
        }
        catch
        {
        }
    }

    private void UpdateMarkdownPreview (string markdownText)
    {
        if ( MarkdownPreview?.CoreWebView2 == null )
            return;

        try
        {
            string html = ConvertMarkdownToHtml(markdownText);
            MarkdownPreview.NavigateToString(html);
        }
        catch
        {
        }
    }

    private string ConvertMarkdownToHtml (string markdown)
    {
        if ( string.IsNullOrWhiteSpace(markdown) )
            return GetEmptyPreviewHtml();

        string htmlBody = Markdown.ToHtml(markdown , _markdownPipeline);

        return WrapWithGitHubStyle(htmlBody);
    }

    private string GetEmptyPreviewHtml ()
    {
        return WrapWithGitHubStyle("<p style='color:#888; text-align:center; margin-top:50px;'>Preview sẽ hiển thị ở đây...</p>");
    }

    private string WrapWithGitHubStyle (string htmlBody)
    {
        return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1'>
    <style>
        /* GitHub Markdown CSS */
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

        /* Dark mode support */
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
