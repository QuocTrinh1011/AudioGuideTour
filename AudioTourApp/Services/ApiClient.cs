using AudioTourApp.Models;
using System.Net.Http.Json;
using System.Text.Json;

namespace AudioTourApp.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        BaseUrl = "http://10.0.2.2:5297";
    }

    public string BaseUrl { get; set; }

    public async Task<List<PoiItem>> GetNearbyAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/map/nearby", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<PoiItem>>(_jsonOptions, cancellationToken) ?? new();
        return items.Select(NormalizePoi).ToList();
    }

    public async Task<GeofenceCheckResponse> CheckGeofenceAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/geofence", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeofenceCheckResponse>(_jsonOptions, cancellationToken) ?? new();
        result.TriggeredPoi = result.TriggeredPoi is null ? null : NormalizePoi(result.TriggeredPoi);
        result.NearbyPois = result.NearbyPois.Select(NormalizePoi).ToList();
        return result;
    }

    public async Task TrackAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/tracking", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<BootstrapResponse> BootstrapAsync(string language, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/bootstrap?language={Uri.EscapeDataString(language)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BootstrapResponse>(_jsonOptions, cancellationToken) ?? new();
        result.Pois = result.Pois.Select(NormalizePoi).ToList();
        result.Tours = result.Tours.Select(NormalizeTour).ToList();
        return result;
    }

    public async Task<QrLookupResponse?> LookupQrAsync(string code, string language, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/qrcode/{Uri.EscapeDataString(code)}?language={Uri.EscapeDataString(language)}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<QrLookupResponse>(_jsonOptions, cancellationToken);
        if (result?.Poi != null)
        {
            result.Poi = NormalizePoi(result.Poi);
        }

        return result;
    }

    public async Task<VisitorProfile> UpsertVisitorAsync(VisitorProfile visitor, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/user", visitor, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitorProfile>(_jsonOptions, cancellationToken) ?? visitor;
    }

    private PoiItem NormalizePoi(PoiItem poi)
    {
        poi.ImageUrl = NormalizeUrl(poi.ImageUrl);
        poi.MapUrl = NormalizeUrl(poi.MapUrl);
        poi.AudioUrl = NormalizeUrl(poi.AudioUrl);
        return poi;
    }

    private TourItem NormalizeTour(TourItem tour)
    {
        tour.CoverImageUrl = NormalizeUrl(tour.CoverImageUrl);
        tour.Stops ??= new List<TourStopItem>();

        foreach (var stop in tour.Stops)
        {
            if (stop.Poi != null)
            {
                stop.Poi = NormalizePoi(stop.Poi);
            }
        }

        return tour;
    }

    private string NormalizeUrl(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            return value;
        }

        return $"{BaseUrl.TrimEnd('/')}/{value.TrimStart('/')}";
    }
}
