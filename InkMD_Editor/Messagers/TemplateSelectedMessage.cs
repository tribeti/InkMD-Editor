namespace InkMD_Editor.Messagers;

public class TemplateSelectedMessage
{
    public string TemplateContent { get; }

    public TemplateSelectedMessage (string templateContent)
    {
        TemplateContent = templateContent;
    }
}