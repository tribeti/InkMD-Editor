namespace InkMD_Editor.Messages;

public record TemplateSelectedMessage (
    string Content ,
    bool CreateNewFile = false
);
