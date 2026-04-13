using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Devices.Sensors;

namespace AudioTourApp.Services;

public class LocationTrackingService
{
    private static readonly TimeSpan MaxLastKnownAge = TimeSpan.FromMinutes(15);
    private const double MaxLastKnownAccuracyMeters = 1000d;
    private const double MaxTrackedAccuracyMeters = 2000d;
    private readonly TrackingForegroundBridge _trackingForegroundBridge;
    private CancellationTokenSource? _cts;
    private Task? _trackingLoopTask;
    private bool _isRunning;
    private bool _isForeground = true;
    private TimeSpan _requestedInterval = TimeSpan.FromSeconds(6);
    private TimeSpan _adaptiveInterval = TimeSpan.FromSeconds(6);
    private Location? _lastLocation;
    private int _consecutiveFailures;

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
                "Audio Tour đang tracking",
                isForeground
                    ? "Đang theo doi vi tri o che do tien canh."
                    : "Đang giu tracking o che do nen.");
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
        _consecutiveFailures = 0;
        await _trackingForegroundBridge.StartAsync(
            "Audio Tour đang tracking",
            "GPS, geofence và tour đang sẵn sàng.");
        await TryPublishLastKnownLocationAsync(_cts.Token);
        _trackingLoopTask = Task.Run(async () => await RunTrackingLoopAsync(_cts.Token));
    }

    public async Task<Location?> GetCurrentLocationAsync(CancellationToken cancellationToken = default)
    {
        if (!await EnsurePermissionsAsync())
        {
            return null;
        }

        var request = new GeolocationRequest(GeolocationAccuracy.Best, TimeSpan.FromSeconds(10));
        var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
        return IsReliableLocation(location, TimeSpan.FromMinutes(5), MaxTrackedAccuracyMeters)
            ? location
            : null;
    }

    public Task StopAsync()
    {
        _cts?.Cancel();
        _cts = null;
        _trackingLoopTask = null;
        _isRunning = false;
        _lastLocation = null;
        _consecutiveFailures = 0;
        _adaptiveInterval = _requestedInterval;
        return _trackingForegroundBridge.StopAsync();
    }

    private TimeSpan GetCurrentInterval()
    {
        var interval = _adaptiveInterval;
        if (_consecutiveFailures > 0)
        {
            interval += TimeSpan.FromSeconds(Math.Min(_consecutiveFailures * 4, 20));
        }

        if (_isForeground)
        {
            return interval;
        }

        return interval < TimeSpan.FromSeconds(20)
            ? TimeSpan.FromSeconds(20)
            : interval;
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
        var accuracy = currentLocation.Accuracy ?? 0;

        var next = speed switch
        {
            >= 4 => TimeSpan.FromSeconds(4),
            >= 1.5 => TimeSpan.FromSeconds(8),
            _ when movedMeters >= 40 => TimeSpan.FromSeconds(6),
            _ when movedMeters >= 15 => TimeSpan.FromSeconds(10),
            _ when accuracy > 0 && accuracy > 120 => TimeSpan.FromSeconds(18),
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

    private async Task RunTrackingLoopAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var currentInterval = GetCurrentInterval();
                    var accuracy = _isForeground ? GeolocationAccuracy.Best : GeolocationAccuracy.Medium;
                    var request = new GeolocationRequest(accuracy, currentInterval);
                    var location = await Geolocation.Default.GetLocationAsync(request, cancellationToken);
                    if (location != null && IsReliableLocation(location, TimeSpan.FromMinutes(5), MaxTrackedAccuracyMeters))
                    {
                        _consecutiveFailures = 0;
                        _adaptiveInterval = ComputeNextInterval(location);
                        _lastLocation = location;
                        LocationChanged?.Invoke(this, location);
                    }
                    else
                    {
                        _consecutiveFailures++;
                    }
                }
                catch
                {
                    _consecutiveFailures++;
                }

                try
                {
                    await Task.Delay(GetCurrentInterval(), cancellationToken);
                }
                catch
                {
                    break;
                }
            }
        }
        finally
        {
            _isRunning = false;
        }
    }

    private async Task TryPublishLastKnownLocationAsync(CancellationToken cancellationToken)
    {
        try
        {
            var location = await Geolocation.Default.GetLastKnownLocationAsync();
            if (location == null ||
                cancellationToken.IsCancellationRequested ||
                !IsReliableLocation(location, MaxLastKnownAge, MaxLastKnownAccuracyMeters))
            {
                return;
            }

            _lastLocation = location;
            LocationChanged?.Invoke(this, location);
        }
        catch
        {
        }
    }

    private static bool IsReliableLocation(Location? location, TimeSpan maxAge, double maxAccuracyMeters)
    {
        if (location == null)
        {
            return false;
        }

        if (location.Latitude is < -90 or > 90 || location.Longitude is < -180 or > 180)
        {
            return false;
        }

        var accuracy = location.Accuracy ?? 0;
        if (accuracy > 0 && accuracy > maxAccuracyMeters)
        {
            return false;
        }

        var timestamp = location.Timestamp;
        if (timestamp != default)
        {
            var age = DateTimeOffset.UtcNow - timestamp.ToUniversalTime();
            if (age > maxAge)
            {
                return false;
            }
        }

        return true;
    }
}
