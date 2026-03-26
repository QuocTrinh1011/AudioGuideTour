using AudioTourApp.Models;
using AudioTourApp.Services;
using Microsoft.Maui.Devices.Sensors;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;

namespace AudioTourApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly ApiClient _apiClient;
    private readonly LocationTrackingService _locationService;
    private readonly AudioQueueService _audioQueueService;
    private string _status = "San sang";
    private string _currentLocation = "Chua co vi tri";
    private string _mapHtml = "<html><body style='font-family:sans-serif;padding:20px'>Khoi dong tracking de tai map.</body></html>";
    private bool _isTracking;
    private string _apiBaseUrl;
    private string _userId = Guid.NewGuid().ToString("N");
    private readonly string _deviceId = DeviceInfo.Current.Idiom + "-" + Guid.NewGuid().ToString("N")[..8];

    public MainViewModel(ApiClient apiClient, LocationTrackingService locationService, AudioQueueService audioQueueService)
    {
        _apiClient = apiClient;
        _locationService = locationService;
        _audioQueueService = audioQueueService;
        _apiBaseUrl = apiClient.BaseUrl;

        _locationService.LocationChanged += OnLocationChanged;
        _audioQueueService.StatusChanged += (_, status) => Status = status;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<PoiItem> NearbyPois { get; } = new();
    public ObservableCollection<TourItem> Tours { get; } = new();

    public string ApiBaseUrl
    {
        get => _apiBaseUrl;
        set => SetField(ref _apiBaseUrl, value);
    }

    public string Status
    {
        get => _status;
        set => SetField(ref _status, value);
    }

    public string CurrentLocation
    {
        get => _currentLocation;
        set => SetField(ref _currentLocation, value);
    }

    public string MapHtml
    {
        get => _mapHtml;
        set => SetField(ref _mapHtml, value);
    }

    public bool IsTracking
    {
        get => _isTracking;
        set => SetField(ref _isTracking, value);
    }

    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        _apiClient.BaseUrl = ApiBaseUrl;
        var bootstrap = await _apiClient.BootstrapAsync("vi-VN", cancellationToken);
        Tours.Clear();
        foreach (var tour in bootstrap.Tours)
        {
            Tours.Add(tour);
        }
        Status = $"Da tai {bootstrap.Pois.Count} POI va {bootstrap.Tours.Count} tour.";
    }

    public async Task ToggleTrackingAsync()
    {
        if (IsTracking)
        {
            await _locationService.StopAsync();
            IsTracking = false;
            Status = "Da dung tracking.";
            return;
        }

        await BootstrapAsync();
        await _locationService.StartAsync(TimeSpan.FromSeconds(6));
        IsTracking = true;
        Status = "Dang tracking foreground. Nen background can bo sung foreground service Android neu can chay lien tuc.";
    }

    private async void OnLocationChanged(object? sender, Location location)
    {
        CurrentLocation = $"{location.Latitude:F6}, {location.Longitude:F6} | acc {location.Accuracy:F1}m";

        var request = new LocationUpdateRequest
        {
            UserId = _userId,
            DeviceId = _deviceId,
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            Accuracy = location.Accuracy,
            SpeedMetersPerSecond = location.Speed,
            Bearing = location.Course,
            IsForeground = true,
            RecordedAt = DateTime.UtcNow
        };

        try
        {
            await _apiClient.TrackAsync(request);

            var nearby = await _apiClient.GetNearbyAsync(request);
            NearbyPois.Clear();
            foreach (var poi in nearby)
            {
                NearbyPois.Add(poi);
            }

            var geofence = await _apiClient.CheckGeofenceAsync(request);
            if (geofence.ShouldPlay && geofence.TriggeredPoi != null)
            {
                await _audioQueueService.EnqueueAsync(geofence.TriggeredPoi);
            }

            UpdateMap(location, nearby);
        }
        catch (Exception ex)
        {
            Status = $"Loi ket noi API: {ex.Message}";
        }
    }

    private void UpdateMap(Location location, List<PoiItem> nearby)
    {
        var html = new StringBuilder();
        html.Append("""
            <html>
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1.0" />
              <link rel="stylesheet" href="https://unpkg.com/leaflet/dist/leaflet.css" />
              <script src="https://unpkg.com/leaflet/dist/leaflet.js"></script>
              <style>html,body,#map{height:100%;margin:0;} .leaflet-popup-content{font-family:sans-serif;}</style>
            </head>
            <body><div id="map"></div><script>
        """);
        html.Append($"const map = L.map('map').setView([{location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}], 17);");
        html.Append("L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png').addTo(map);");
        html.Append($"L.marker([{location.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {location.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}]).addTo(map).bindPopup('Ban dang o day');");

        foreach (var poi in nearby)
        {
            html.Append($"L.marker([{poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}, {poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}]).addTo(map).bindPopup('<b>{Escape(poi.Title)}</b><br/>{Escape(poi.Summary)}<br/>{poi.DistanceMeters:F0}m');");
        }

        html.Append("</script></body></html>");
        MapHtml = html.ToString();
    }

    private static string Escape(string value) => value.Replace("'", "\\'");

    private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (Equals(field, value))
        {
            return;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
