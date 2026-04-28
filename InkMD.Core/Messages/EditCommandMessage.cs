namespace InkMD.Core.Messages;

public enum EditCommandType
{
    Undo,
    Redo,
    Cut,
    Copy,
    Paste,
    Bold,
    Italic,
    Strikethrough
}

public record EditCommandMessage(EditCommandType Command);
