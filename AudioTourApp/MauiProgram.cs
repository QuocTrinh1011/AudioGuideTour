using AudioTourApp.Pages;
using AudioTourApp.Services;
using AudioTourApp.ViewModels;
using BarcodeScanning;
using Microsoft.Extensions.Logging;

namespace AudioTourApp;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseBarcodeScanning()
            .UseMauiMaps();

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
        builder.Services.AddSingleton<AppPermissionService>();
        builder.Services.AddSingleton<AudioQueueService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<AppShell>();
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddSingleton<HomePage>();
        builder.Services.AddSingleton<PoiPage>();
        builder.Services.AddSingleton<QrPage>();
        builder.Services.AddTransient<QrScannerPage>();
        builder.Services.AddSingleton<MapPage>();
        builder.Services.AddSingleton<ToursPage>();
        builder.Services.AddSingleton<SettingsPage>();
        builder.Services.AddTransient<RegistrationPage>();
        builder.Services.AddTransient<RegistrationPlanPage>();
        builder.Services.AddTransient<PoiDetailPage>();
        builder.Services.AddTransient<NarrationPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
