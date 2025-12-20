namespace InkMD_Editor.Messagers;

public class FontChangedMessage (string fontFamily , int fontSize)
{
    public string FontFamily { get; } = fontFamily;
    public int FontSize { get; } = fontSize;
}
