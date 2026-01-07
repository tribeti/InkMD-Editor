using InkMD_Editor.Services;
using InkMD_Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace InkMD_Editor;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; }

    public App ()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    private static ServiceProvider ConfigureServices ()
    {
        var services = new ServiceCollection();

        // Register Services
        services.AddSingleton<IFileService , FileService>();
        services.AddSingleton<IDialogService , DialogService>();
        services.AddSingleton<ThemeService>();

        // Register ViewModels
        services.AddTransient<EditorPageViewModel>();
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<TabViewContentViewModel>();
        services.AddTransient<EditTabViewModel>();

        // Register Views (Pages)
        services.AddSingleton<MainWindow>();
        services.AddTransient<Views.EditorPage>();
        services.AddTransient<Views.SettingsPage>();

        return services.BuildServiceProvider();
    }

    protected override void OnLaunched (LaunchActivatedEventArgs args)
    {
        MainWindow = Services.GetRequiredService<MainWindow>();
        Services.GetRequiredService<ThemeService>().ApplyTheme(MainWindow);
        MainWindow.Activate();
    }
}