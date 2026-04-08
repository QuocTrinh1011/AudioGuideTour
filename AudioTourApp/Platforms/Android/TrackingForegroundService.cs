using Android.App;
using Android.Content;
using Android.OS;
using Android.Content.PM;
#pragma warning disable CA1416

namespace AudioTourApp;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeLocation)]
public class TrackingForegroundService : Service
{
    public const string ChannelId = "audio-tour-tracking";
    public const int NotificationId = 4107;
    public const string ActionStart = "audio.tour.tracking.start";
    public const string ActionUpdate = "audio.tour.tracking.update";
    public const string ActionStop = "audio.tour.tracking.stop";
    public const string ExtraTitle = "title";
    public const string ExtraText = "text";

    public override void OnCreate()
    {
        base.OnCreate();
        CreateNotificationChannel();
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? ActionStart;
        var title = intent?.GetStringExtra(ExtraTitle) ?? "Audio Tour đang tracking";
        var text = intent?.GetStringExtra(ExtraText) ?? "GPS, geofence và thuyết minh đang sẵn sàng.";

        switch (action)
        {
            case ActionStop:
                StopForeground(StopForegroundFlags.Remove);
                StopSelf();
                return StartCommandResult.NotSticky;
            case ActionUpdate:
                var manager = GetSystemService(NotificationService) as NotificationManager;
                manager?.Notify(NotificationId, BuildNotification(title, text));
                return StartCommandResult.Sticky;
            default:
                StartForeground(NotificationId, BuildNotification(title, text));
                return StartCommandResult.Sticky;
        }
    }

    private Notification BuildNotification(string title, string text)
    {
        var intent = new Intent(this, typeof(MainActivity));
        intent.AddFlags(ActivityFlags.SingleTop | ActivityFlags.ClearTop);
        var flags = PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent;
        var pendingIntent = PendingIntent.GetActivity(this, 0, intent, flags);

        return new Notification.Builder(this, ChannelId)
            .SetContentTitle(title)
            .SetContentText(text)
            .SetSmallIcon(Android.Resource.Drawable.IcMediaPlay)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetContentIntent(pendingIntent)
            .Build();
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt < BuildVersionCodes.O)
        {
            return;
        }

        var manager = GetSystemService(NotificationService) as NotificationManager;
        if (manager?.GetNotificationChannel(ChannelId) != null)
        {
            return;
        }

        var channel = new NotificationChannel(ChannelId, "Audio Tour Tracking", NotificationImportance.Low)
        {
            Description = "Trang thai tracking GPS va geofence cua Audio Tour."
        };

        manager?.CreateNotificationChannel(channel);
    }
}

#pragma warning restore CA1416
