namespace InkMD_Editor.Messages;

public enum EditCommandType
{
    Undo,
    Redo,
    Cut,
    Copy,
    Paste
}

public record EditCommandMessage (EditCommandType Command);