using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace AudioTourApp.Services;

public class LocationTrackingService
{
    private CancellationTokenSource? _cts;
    private bool _isRunning;

    public event EventHandler<Location>? LocationChanged;

    public bool IsRunning => _isRunning;

    public async Task<bool> EnsurePermissionsAsync()
    {
        var whenInUse = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        return whenInUse == PermissionStatus.Granted;
    }

    public async Task StartAsync(TimeSpan interval)
    {
        if (_isRunning)
        {
            return;
        }

        if (!await EnsurePermissionsAsync())
        {
            throw new InvalidOperationException("Location permission was not granted.");
        }

        _cts = new CancellationTokenSource();
        _isRunning = true;

        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var location = await Geolocation.Default.GetLocationAsync(new GeolocationRequest(GeolocationAccuracy.Best, interval), _cts.Token);
                    if (location != null)
                    {
                        LocationChanged?.Invoke(this, location);
                    }
                }
                catch
                {
                }

                try
                {
                    await Task.Delay(interval, _cts.Token);
                }
                catch
                {
                    break;
                }
            }

            _isRunning = false;
        });
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _isRunning = false;
        return Task.CompletedTask;
    }
}
