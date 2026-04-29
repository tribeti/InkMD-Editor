using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;

namespace InkMD_Editor;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        AppWindow.Resize(new Windows.Graphics.SizeInt32(1600, 1000));
        InitializeComponent();
        AppWindow.TitleBar.PreferredTheme = TitleBarTheme.UseDefaultAppMode;
        AppWindow.Title = "InkMD Editor";
        ContentFrame.Navigate(typeof(Views.EditorPage));
    }
}
