using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices;

namespace AudioTourApp.Services;

public class AppPermissionService
{
    public async Task<AppPermissionSnapshot> GetStatusAsync()
    {
        var locationWhenInUse = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();
        PermissionStatus? locationAlways = null;
        try
        {
            locationAlways = await Permissions.CheckStatusAsync<Permissions.LocationAlways>();
        }
        catch
        {
        }

        PermissionStatus? camera = null;
        try
        {
            camera = await Permissions.CheckStatusAsync<Permissions.Camera>();
        }
        catch
        {
        }

        PermissionStatus? notifications = null;
        try
        {
            notifications = await Permissions.CheckStatusAsync<Permissions.PostNotifications>();
        }
        catch
        {
        }

        return new AppPermissionSnapshot(locationWhenInUse, locationAlways, camera, notifications);
    }

    public async Task<AppPermissionSnapshot> RequestTrackingPermissionsAsync()
    {
        await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        try
        {
            await Permissions.RequestAsync<Permissions.LocationAlways>();
        }
        catch
        {
        }

        try
        {
            if (DeviceInfo.Platform == DevicePlatform.Android)
            {
                await Permissions.RequestAsync<Permissions.PostNotifications>();
            }
        }
        catch
        {
        }

        return await GetStatusAsync();
    }

    public async Task<AppPermissionSnapshot> RequestCameraPermissionAsync()
    {
        try
        {
            await Permissions.RequestAsync<Permissions.Camera>();
        }
        catch
        {
        }

        return await GetStatusAsync();
    }

    public Task OpenSystemSettingsAsync()
    {
        AppInfo.Current.ShowSettingsUI();
        return Task.CompletedTask;
    }
}

public sealed record AppPermissionSnapshot(
    PermissionStatus LocationWhenInUse,
    PermissionStatus? LocationAlways,
    PermissionStatus? Camera,
    PermissionStatus? Notifications);
