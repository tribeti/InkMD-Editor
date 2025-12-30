namespace InkMD_Editor.Messages;

public record ContentChangedMessage (string FilePath , bool IsDirty);