using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using InkMD_Editor.Helpers;
using InkMD_Editor.Messagers;

namespace InkMD_Editor.ViewModels;

public partial class EditTabViewModel : ObservableObject, IRecipient<FontChangedMessage>
{
    [ObservableProperty]
    public partial string? FileName { get; set; }

    [ObservableProperty]
    public partial string? FilePath { get; set; }

    [ObservableProperty]
    public partial string? CurrentContent { get; set; }

    [ObservableProperty]
    public partial string FontFamily { get; set; } = AppSettings.GetFontFamily();

    [ObservableProperty]
    public partial double FontSize { get; set; } = AppSettings.GetFontSize();

    public bool IsSaved => !string.IsNullOrEmpty(FilePath);

    public EditTabViewModel ()
    {
        FileName = "Untitled";
        WeakReferenceMessenger.Default.Register(this);
    }

    public void SetFilePath (string path , string name)
    {
        FilePath = path;
        FileName = name;
    }

    public void Receive (FontChangedMessage message)
    {
        FontFamily = message.FontFamily;
        FontSize = message.FontSize;
    }
}
