using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls;
using Avalonia.Threading;

using Microsoft.Extensions.DependencyInjection;
using Avalonia;
using SampleAvaloniaApplication.Views;
using SampleAvaloniaApplication.ViewModels;

namespace SampleAvaloniaApplication;

public static class ServiceCollectionExtensions
{
    public static void AddServices(this IServiceCollection services)
    {
        services.AddAvaloniaServices();
        services.AddViews();
    }

    private static void AddAvaloniaServices(this IServiceCollection services)
    {
        services.AddSingleton<IDispatcher>(_ => Dispatcher.UIThread);
        services.AddSingleton(_ => Application.Current?.ApplicationLifetime ?? throw new InvalidOperationException("No application lifetime is set"));

        services.AddSingleton(sp =>
            sp.GetRequiredService<IApplicationLifetime>() switch
            {
                IClassicDesktopStyleApplicationLifetime desktop => desktop.MainWindow ?? throw new InvalidOperationException("No main window set"),
                ISingleViewApplicationLifetime singleViewPlatform => TopLevel.GetTopLevel(singleViewPlatform.MainView) ?? throw new InvalidOperationException("Could not find top level element for single view"),
                _ => throw new InvalidOperationException($"Could not find {nameof(TopLevel)} element"),
            }
        );

        services.AddSingleton(sp => sp.GetRequiredService<TopLevel>().StorageProvider);
    }

    private static void AddViews(this IServiceCollection services)
    {
        //NB: Window is only needed for Desktop
        services.AddTransient<MainWindow>();

        services.AddView<MainView, MainViewModel>();
    }

    private static void AddView<TView, TViewModel>(this IServiceCollection services)
        where TView : class
        where TViewModel : class
    {
        services.AddTransient<TViewModel>();
        services.AddTransient<TView>();
    }
}
