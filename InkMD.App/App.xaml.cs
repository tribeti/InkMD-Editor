using InkMD.App.Services;
using InkMD.App.ViewModels;
using InkMD.Core.Services;
using InkMD_Editor.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;

namespace InkMD.App;

public partial class App : Application
{
    public static Window? MainWindow { get; private set; }
    public IServiceProvider Services { get; }

    public App()
    {
        Services = ConfigureServices();
        InitializeComponent();
    }

    private static ServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register Services
        services.AddSingleton<IFileService, FileService>();
        services.AddSingleton<IDialogService, DialogService>();
        services.AddSingleton<ThemeService>();
        services.AddSingleton<WebView2EnvironmentService>();

        // Register ViewModels & Views
        services.AddTransient<EditorPageViewModel>();
        services.AddTransient<MainMenuViewModel>();
        services.AddTransient<TabViewContentViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddTransient<Views.EditorPage>();
        services.AddTransient<Views.SettingsPage>();

        return services.BuildServiceProvider();
    }

    protected override async void OnLaunched(LaunchActivatedEventArgs args)
    {
        await Services.GetRequiredService<WebView2EnvironmentService>()
                .InitializeAsync();

        MainWindow = Services.GetRequiredService<MainWindow>();
        Services.GetRequiredService<ThemeService>().ApplyTheme(MainWindow);
        MainWindow.Activate();
    }
}