namespace InkMD_Editor.Helpers;

public class GitHubPreview
{
    public static string GetEmptyPreviewHtml ()
    {
        return WrapWithGitHubStyle("<p style='color:#888; text-align:center; margin-top:50px;'>Preview will show here...</p>");
    }

    public static string WrapWithGitHubStyle (string htmlBody)
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
