using System.IO;
using CombasLauncherApp.Services.Implementations;
using CombasLauncherApp.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace CombasLauncherApp.Services;

public static class ServiceProvider
{
    private static IServiceProvider _serviceProvider;

    public static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Register your services here
        
        services.AddSingleton<IMessageBoxService, MessageBoxService>();
        services.AddSingleton<ILoggingService>(_ => new LoggingService(Path.Combine(AppService.LocalAppData, "Logs")));
        services.AddSingleton<IXeniaService, XeniaService>();
        services.AddSingleton<ITailScaleService, TailScaleService>();
        services.AddSingleton<INavigationService, NavigationService>();
        //services.AddSingleton<IUpdateService, UpdateService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    public static T GetService<T>() => _serviceProvider.GetService<T>();
}