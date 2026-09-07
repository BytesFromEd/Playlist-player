using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using UI.Services;
using UI.ViewModels;

namespace UI;

public partial class App : Application
{
    private static IServiceProvider? Services { get; set; }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();

        services.AddSingleton<State>();
        services.AddTransient<MainViewModel>();
        services.AddTransient<PlaylistViewModel>();
        services.AddTransient<SettingsViewModel>();

        Services = services.BuildServiceProvider();


        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var mainViewModel = Services.GetRequiredService<MainViewModel>();
            var state = Services.GetRequiredService<State>();
            
            state.mainView = new Views.MainView()
            {
                DataContext = mainViewModel
            };
            
            desktop.MainWindow = state.mainView;
            mainViewModel.Load();
        }

        base.OnFrameworkInitializationCompleted();
    }
}