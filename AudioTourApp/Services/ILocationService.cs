namespace AudioTourApp.Services;

public interface ILocationService
{
    Task StartTrackingAsync();
    Task StopTrackingAsync();
    event Action<Location> OnLocationChanged;
}