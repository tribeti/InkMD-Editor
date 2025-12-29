namespace InkMD_Editor.Messagers;

public enum EditCommandType
{
    Undo,
    Redo,
    Cut,
    Copy,
    Paste
}

public record EditCommandMessage (EditCommandType Command);