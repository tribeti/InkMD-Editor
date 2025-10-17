using InkMD_Editor.ViewModels;
using Microsoft.UI.Xaml.Controls;

namespace InkMD_Editor.Controls;

public sealed partial class MainMenu : UserControl
{
    public MenuBarViewModel? MenuBarViewModel { get; } = new();
    public MainMenu ()
    {
        InitializeComponent();
    }
}
