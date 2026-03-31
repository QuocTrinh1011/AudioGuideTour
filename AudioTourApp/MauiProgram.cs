using AudioTourApp.Pages;
using AudioTourApp.Services;
using AudioTourApp.ViewModels;
using Microsoft.Extensions.Logging;

namespace AudioTourApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>();

        builder.Services.AddSingleton(new HttpClient(new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        }));
        builder.Services.AddSingleton<ApiClient>();
        builder.Services.AddSingleton<TrackingForegroundBridge>();
        builder.Services.AddSingleton<LocationTrackingService>();
        builder.Services.AddSingleton<AudioInterruptionService>();
        builder.Services.AddSingleton<AudioFallbackPlayer>();
        builder.Services.AddSingleton<NarrationService>();
        builder.Services.AddSingleton<AudioQueueService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<MapPage>();
        builder.Services.AddSingleton<ToursPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddTransient<PoiDetailPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
