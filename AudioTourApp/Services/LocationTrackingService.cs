using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace AudioTourApp.Services;

public class LocationTrackingService
{
    private readonly TrackingForegroundBridge _trackingForegroundBridge;
    private CancellationTokenSource? _cts;
    private bool _isRunning;
    private bool _isForeground = true;
    private TimeSpan _requestedInterval = TimeSpan.FromSeconds(6);
    private TimeSpan _adaptiveInterval = TimeSpan.FromSeconds(6);
    private Location? _lastLocation;

    public event EventHandler<Location>? LocationChanged;

    public bool IsRunning => _isRunning;

    public LocationTrackingService(TrackingForegroundBridge trackingForegroundBridge)
    {
        _trackingForegroundBridge = trackingForegroundBridge;
    }

    public async Task<bool> EnsurePermissionsAsync()
    {
        var whenInUse = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        if (whenInUse != PermissionStatus.Granted)
        {
            return false;
        }

        try
        {
            await Permissions.RequestAsync<Permissions.LocationAlways>();
        }
        catch
        {
        }

        return true;
    }

    public void SetForegroundState(bool isForeground)
    {
        _isForeground = isForeground;
        if (_isRunning)
        {
            _ = _trackingForegroundBridge.UpdateAsync(
                "Audio Tour dang tracking",
                isForeground
                    ? "Dang theo doi vi tri o che do tien canh."
                    : "Dang giu tracking o che do nen.");
        }
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
        _requestedInterval = interval;
        _adaptiveInterval = interval;
        _lastLocation = null;
        await _trackingForegroundBridge.StartAsync(
            "Audio Tour dang tracking",
            "GPS, geofence va tour dang san sang.");

        _ = Task.Run(async () =>
        {
            while (!_cts.IsCancellationRequested)
            {
                try
                {
                    var currentInterval = GetCurrentInterval();
                    var accuracy = _isForeground ? GeolocationAccuracy.Best : GeolocationAccuracy.Medium;
                    var request = new GeolocationRequest(accuracy, currentInterval);
                    var location = await Geolocation.Default.GetLocationAsync(request, _cts.Token);
                    if (location != null)
                    {
                        _adaptiveInterval = ComputeNextInterval(location);
                        _lastLocation = location;
                        LocationChanged?.Invoke(this, location);
                    }
                }
                catch
                {
                }

                try
                {
                    await Task.Delay(GetCurrentInterval(), _cts.Token);
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
        _cts = null;
        _isRunning = false;
        _lastLocation = null;
        _adaptiveInterval = _requestedInterval;
        return _trackingForegroundBridge.StopAsync();
    }

    private TimeSpan GetCurrentInterval()
    {
        if (_isForeground)
        {
            return _adaptiveInterval;
        }

        return _adaptiveInterval < TimeSpan.FromSeconds(20)
            ? TimeSpan.FromSeconds(20)
            : _adaptiveInterval;
    }

    private TimeSpan ComputeNextInterval(Location currentLocation)
    {
        var speed = currentLocation.Speed ?? 0;
        var movedMeters = _lastLocation == null
            ? 0
            : Location.CalculateDistance(
                _lastLocation.Latitude,
                _lastLocation.Longitude,
                currentLocation.Latitude,
                currentLocation.Longitude,
                DistanceUnits.Kilometers) * 1000;

        var next = speed switch
        {
            >= 4 => TimeSpan.FromSeconds(4),
            >= 1.5 => TimeSpan.FromSeconds(8),
            _ when movedMeters >= 40 => TimeSpan.FromSeconds(6),
            _ when movedMeters >= 15 => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(15)
        };

        if (!_isForeground && next < TimeSpan.FromSeconds(20))
        {
            next = TimeSpan.FromSeconds(20);
        }

        if (next < _requestedInterval && _isForeground)
        {
            return next;
        }

        return next > _requestedInterval && _isForeground
            ? next
            : (_isForeground ? _requestedInterval : next);
    }
}
