using System.Diagnostics;
#pragma warning disable CA1416

#if ANDROID
using Android.App;
using Android.Content;
using Java.Lang;
#endif

namespace AudioTourApp.Services;

public class TrackingForegroundBridge
{
    public Task StartAsync(string title, string text)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, Class.FromType(typeof(TrackingForegroundService)));
            intent.SetAction(TrackingForegroundService.ActionStart);
            intent.PutExtra(TrackingForegroundService.ExtraTitle, title);
            intent.PutExtra(TrackingForegroundService.ExtraText, text);
            context.StartForegroundService(intent);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Cannot start foreground service: {ex}");
        }
#endif
        return Task.CompletedTask;
    }

    public Task UpdateAsync(string title, string text)
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, Class.FromType(typeof(TrackingForegroundService)));
            intent.SetAction(TrackingForegroundService.ActionUpdate);
            intent.PutExtra(TrackingForegroundService.ExtraTitle, title);
            intent.PutExtra(TrackingForegroundService.ExtraText, text);
            context.StartService(intent);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Cannot update foreground service: {ex}");
        }
#endif
        return Task.CompletedTask;
    }

    public Task StopAsync()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var intent = new Intent(context, Class.FromType(typeof(TrackingForegroundService)));
            intent.SetAction(TrackingForegroundService.ActionStop);
            context.StartService(intent);
        }
        catch (System.Exception ex)
        {
            Debug.WriteLine($"Cannot stop foreground service: {ex}");
        }
#endif
        return Task.CompletedTask;
    }
}

#pragma warning restore CA1416
