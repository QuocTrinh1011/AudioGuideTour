using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.ApplicationModel;
namespace AudioTourApp.Services;

public class LocationService : ILocationService
{
    public event Action<Location> OnLocationChanged;

    private CancellationTokenSource _cts;
    private bool _isTracking = false;

    public async Task StartTrackingAsync()
    {
        if (_isTracking) return;

        _isTracking = true;
        _cts = new CancellationTokenSource();

        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                var request = new GeolocationRequest(
                    GeolocationAccuracy.High,
                    TimeSpan.FromSeconds(5)
                );

                var location = await Geolocation.GetLocationAsync(request);

                if (location != null)
                {
                    OnLocationChanged?.Invoke(location);
                }

                await Task.Delay(3000);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }
    }

    public Task StopTrackingAsync()
    {
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}