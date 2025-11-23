using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl
{
    public StoragePickerViewModel? StoragePickerViewModel { get; } = new();
    
    public MainMenu ()
    {
        InitializeComponent();
    }

    public void SetVisibility (bool isVisible)
    {
        DisplayMode.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
    }
}
