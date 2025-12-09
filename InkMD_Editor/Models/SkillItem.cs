namespace InkMD_Editor.Models;

public class SkillItem
{
    public string Name { get; set; }
    public string MarkdownContent => $"![](https://skillicons.dev/icons?i={Name})";

    public SkillItem (string name)
    {
        Name = name;
    }
}