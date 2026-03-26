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
        BaseUrl = "http://10.0.2.2:5000";
    }

    public string BaseUrl { get; set; }

    public async Task<List<PoiItem>> GetNearbyAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/map/nearby", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<List<PoiItem>>(_jsonOptions, cancellationToken) ?? new();
    }

    public async Task<GeofenceCheckResponse> CheckGeofenceAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/geofence", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<GeofenceCheckResponse>(_jsonOptions, cancellationToken) ?? new();
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
        return await response.Content.ReadFromJsonAsync<BootstrapResponse>(_jsonOptions, cancellationToken) ?? new();
    }
}
