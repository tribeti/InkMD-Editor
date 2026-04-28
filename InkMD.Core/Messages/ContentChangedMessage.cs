namespace InkMD.Core.Messages;

public record ContentChangedMessage(string FilePath, bool IsDirty);
