namespace InkMD_Editor.Messages;

public class FontChangedMessage (string fontFamily , double fontSize)
{
    public string FontFamily { get; } = fontFamily;
    public double FontSize { get; } = fontSize;
}
