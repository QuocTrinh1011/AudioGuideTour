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
        builder.Services.AddSingleton<LocationTrackingService>();
        builder.Services.AddSingleton<AudioQueueService>();
        builder.Services.AddSingleton<MainViewModel>();
        builder.Services.AddSingleton<MainPage>();

#if DEBUG
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
