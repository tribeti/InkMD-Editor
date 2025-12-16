using InkMD_Editor.Services;
using Microsoft.UI.Xaml;

namespace InkMD_Editor;

sealed partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public App ()
    {
        InitializeComponent();

    }

    protected override void OnLaunched (LaunchActivatedEventArgs args)
    {
        MainWindow = new MainWindow();
        ThemeService.ApplyTheme(MainWindow);
        MainWindow.Activate();
    }
}
