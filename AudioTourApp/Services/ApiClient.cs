using AudioTourApp.Models;
using Microsoft.Maui.Storage;
using System.Globalization;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace AudioTourApp.Services;

public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private string _assetVersion = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);

    public ApiClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.Timeout = TimeSpan.FromSeconds(20);
        BaseUrl = "http://10.0.2.2:5297";
    }

    public string BaseUrl { get; set; }

    public async Task<List<PoiItem>> GetNearbyAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        BumpAssetVersion();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/map/nearby", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var items = await response.Content.ReadFromJsonAsync<List<PoiItem>>(_jsonOptions, cancellationToken) ?? new();
        var normalized = items.Select(NormalizePoi).ToList();
        await PreparePoiAssetsAsync(normalized, cancellationToken);
        return normalized;
    }

    public async Task<GeofenceCheckResponse> CheckGeofenceAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        BumpAssetVersion();
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/geofence", request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<GeofenceCheckResponse>(_jsonOptions, cancellationToken) ?? new();
        result.TriggeredPoi = result.TriggeredPoi is null ? null : NormalizePoi(result.TriggeredPoi);
        result.NearbyPois = result.NearbyPois.Select(NormalizePoi).ToList();
        await PreparePoiAssetsAsync(result.NearbyPois, cancellationToken);
        if (result.TriggeredPoi != null)
        {
            await PreparePoiAssetsAsync(new[] { result.TriggeredPoi }, cancellationToken);
        }
        return result;
    }

    public async Task TrackAsync(LocationUpdateRequest request, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/tracking", request, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<BootstrapResponse> BootstrapAsync(string language, CancellationToken cancellationToken = default)
    {
        BumpAssetVersion();
        var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/bootstrap?language={Uri.EscapeDataString(language)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<BootstrapResponse>(_jsonOptions, cancellationToken) ?? new();
        result.Pois = result.Pois.Select(NormalizePoi).ToList();
        result.Tours = result.Tours.Select(NormalizeTour).ToList();
        await PreparePoiAssetsAsync(result.Pois, cancellationToken);
        await PrepareTourAssetsAsync(result.Tours, cancellationToken);
        return result;
    }

    public async Task<QrLookupResponse?> LookupQrAsync(string code, string language, CancellationToken cancellationToken = default)
    {
        BumpAssetVersion();
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
            await PreparePoiAssetsAsync(new[] { result.Poi }, cancellationToken);
        }

        return result;
    }

    public async Task<List<QrDirectoryItem>> GetQrDirectoryAsync(string language, CancellationToken cancellationToken = default)
    {
        BumpAssetVersion();
        var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/qrcode?language={Uri.EscapeDataString(language)}", cancellationToken);
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<List<QrDirectoryItem>>(_jsonOptions, cancellationToken) ?? new();

        foreach (var item in result)
        {
            if (item.Poi != null)
            {
                item.Poi = NormalizePoi(item.Poi);
            }
        }

        await PreparePoiAssetsAsync(result.Where(x => x.Poi != null).Select(x => x.Poi!), cancellationToken);
        return result;
    }

    public async Task<VisitorProfile> UpsertVisitorAsync(VisitorProfile visitor, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/user", visitor, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<VisitorProfile>(_jsonOptions, cancellationToken) ?? visitor;
    }

    public async Task<CustomerSessionItem> LoginCustomerAsync(CustomerLoginPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/customerauth/login", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerSessionItem>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API không trả về phiên đăng nhập hợp lệ.");
    }

    public async Task<CustomerSessionItem?> ValidateCustomerSessionAsync(string accountId, string sessionToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(accountId) || string.IsNullOrWhiteSpace(sessionToken))
        {
            return null;
        }

        var response = await _httpClient.GetAsync(
            $"{BaseUrl.TrimEnd('/')}/api/customerauth/session?accountId={Uri.EscapeDataString(accountId)}&token={Uri.EscapeDataString(sessionToken)}",
            cancellationToken);

        if (response.StatusCode is System.Net.HttpStatusCode.Unauthorized or System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CustomerSessionItem>(_jsonOptions, cancellationToken);
    }

    public async Task LogoutCustomerAsync(CustomerLogoutPayload payload, CancellationToken cancellationToken = default)
    {
        await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/customerauth/logout", payload, cancellationToken);
    }

    public async Task<RegistrationBootstrapItem> GetRegistrationBootstrapAsync(string visitorId, string deviceId, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.GetAsync(
            $"{BaseUrl.TrimEnd('/')}/api/registration/bootstrap?visitorId={Uri.EscapeDataString(visitorId ?? string.Empty)}&deviceId={Uri.EscapeDataString(deviceId ?? string.Empty)}",
            cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationBootstrapItem>(_jsonOptions, cancellationToken) ?? new RegistrationBootstrapItem();
    }

    public async Task<RegistrationStatusItem> SubmitRegistrationFormAsync(SubmitRegistrationFormPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/registration/form", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationStatusItem>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API không trả về hồ sơ đăng ký.");
    }

    public async Task<RegistrationStatusItem> CreateRegistrationPaymentAsync(string registrationId, CreateRegistrationPaymentPayload payload, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/registration/{Uri.EscapeDataString(registrationId)}/payment", payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationStatusItem>(_jsonOptions, cancellationToken)
            ?? throw new InvalidOperationException("API không trả về link thanh toán.");
    }

    public async Task<RegistrationStatusItem?> GetRegistrationStatusAsync(string registrationId, bool refresh = false, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
        {
            return null;
        }

        var response = await _httpClient.GetAsync($"{BaseUrl.TrimEnd('/')}/api/registration/{Uri.EscapeDataString(registrationId)}?refresh={refresh.ToString().ToLowerInvariant()}", cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationStatusItem>(_jsonOptions, cancellationToken);
    }

    public async Task<RegistrationStatusItem?> RefreshRegistrationPaymentAsync(string registrationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(registrationId))
        {
            return null;
        }

        var response = await _httpClient.PostAsync($"{BaseUrl.TrimEnd('/')}/api/registration/{Uri.EscapeDataString(registrationId)}/refresh", null, cancellationToken);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RegistrationStatusItem>(_jsonOptions, cancellationToken);
    }

    public async Task SaveVisitAsync(VisitHistoryRequest visit, CancellationToken cancellationToken = default)
    {
        var response = await _httpClient.PostAsJsonAsync($"{BaseUrl.TrimEnd('/')}/api/visit", visit, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<UrlProbeResult> ProbeUrlAsync(string url, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(url);
        using var request = new HttpRequestMessage(HttpMethod.Get, normalizedUrl);
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        return new UrlProbeResult(
            normalizedUrl,
            (int)response.StatusCode,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            response.Content.Headers.ContentLength);
    }

    public async Task<DownloadedFileResult> DownloadFileToCacheAsync(string url, string cachePrefix, CancellationToken cancellationToken = default)
    {
        var normalizedUrl = NormalizeUrl(url);
        using var response = await _httpClient.GetAsync(normalizedUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        var extension = GuessExtension(normalizedUrl, response.Content.Headers.ContentType?.MediaType);
        var safePrefix = string.IsNullOrWhiteSpace(cachePrefix) ? "audio-tour" : cachePrefix.Trim();
        var fileName = $"{safePrefix}-{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(FileSystem.CacheDirectory, fileName);

        await using var source = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var destination = File.Create(destinationPath);
        await source.CopyToAsync(destination, cancellationToken);

        return new DownloadedFileResult(
            normalizedUrl,
            destinationPath,
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            response.Content.Headers.ContentLength);
    }

    private PoiItem NormalizePoi(PoiItem poi)
    {
        poi.ImageUrl = BuildPoiImageUrl(poi);
        poi.MapUrl = string.IsNullOrWhiteSpace(poi.MapUrl) && poi.Latitude != 0 && poi.Longitude != 0
            ? $"https://www.google.com/maps/search/?api=1&query={poi.Latitude.ToString(System.Globalization.CultureInfo.InvariantCulture)},{poi.Longitude.ToString(System.Globalization.CultureInfo.InvariantCulture)}"
            : NormalizeUrl(poi.MapUrl);
        poi.AudioUrl = NormalizeUrl(poi.AudioUrl);
        return poi;
    }

    private TourItem NormalizeTour(TourItem tour)
    {
        tour.CoverImageUrl = BuildTourCoverUrl(tour);
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

    private async Task PreparePoiAssetsAsync(IEnumerable<PoiItem> pois, CancellationToken cancellationToken)
    {
        foreach (var poi in pois)
        {
            poi.ImageUrl = await MaterializeImagePathAsync(poi.ImageUrl, "poi", cancellationToken);
        }
    }

    private async Task PrepareTourAssetsAsync(IEnumerable<TourItem> tours, CancellationToken cancellationToken)
    {
        foreach (var tour in tours)
        {
            tour.CoverImageUrl = await MaterializeImagePathAsync(tour.CoverImageUrl, "tour", cancellationToken);

            foreach (var stop in tour.Stops)
            {
                if (stop.Poi != null)
                {
                    stop.Poi.ImageUrl = await MaterializeImagePathAsync(stop.Poi.ImageUrl, "poi", cancellationToken);
                }
            }
        }
    }

    private async Task<string> MaterializeImagePathAsync(string? value, string cachePrefix, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : Uri.TryCreate(value, UriKind.Absolute, out _)
                ? value
                : NormalizeAssetUrl(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var absolute) ||
            !(absolute.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
              absolute.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)))
        {
            return normalized;
        }

        try
        {
            return await GetOrDownloadCachedAssetAsync(absolute, cachePrefix, cancellationToken);
        }
        catch
        {
            return normalized;
        }
    }

    private async Task<string> GetOrDownloadCachedAssetAsync(Uri assetUri, string cachePrefix, CancellationToken cancellationToken)
    {
        var cacheDirectory = Path.Combine(FileSystem.CacheDirectory, "images");
        Directory.CreateDirectory(cacheDirectory);

        var assetIdentity = assetUri.GetLeftPart(UriPartial.Path).ToLowerInvariant();
        var extension = Path.GetExtension(assetUri.AbsolutePath);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".img";
        }

        var fileName = $"{cachePrefix}-{ComputeSha256(assetIdentity)}{extension}";
        var destinationPath = Path.Combine(cacheDirectory, fileName);
        var versionPath = $"{destinationPath}.version";
        var requestedVersion = ReadVersionToken(assetUri);

        if (File.Exists(destinationPath))
        {
            var cachedVersion = File.Exists(versionPath) ? File.ReadAllText(versionPath).Trim() : string.Empty;
            if (string.Equals(cachedVersion, requestedVersion, StringComparison.Ordinal))
            {
                return destinationPath;
            }
        }

        var tempPath = $"{destinationPath}.download";
        using var response = await _httpClient.GetAsync(assetUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using (var source = await response.Content.ReadAsStreamAsync(cancellationToken))
        await using (var destination = File.Create(tempPath))
        {
            await source.CopyToAsync(destination, cancellationToken);
        }

        if (File.Exists(destinationPath))
        {
            File.Delete(destinationPath);
        }

        File.Move(tempPath, destinationPath);
        File.WriteAllText(versionPath, requestedVersion);
        return destinationPath;
    }

    private string BuildPoiImageUrl(PoiItem poi)
    {
        if (string.IsNullOrWhiteSpace(poi.ImageUrl))
        {
            return string.Empty;
        }

        if (poi.Id <= 0)
        {
            return NormalizeAssetUrl(poi.ImageUrl);
        }

        return NormalizeAssetUrl($"{BaseUrl.TrimEnd('/')}/api/poi/{poi.Id}/image");
    }

    private string BuildTourCoverUrl(TourItem tour)
    {
        if (string.IsNullOrWhiteSpace(tour.CoverImageUrl))
        {
            return string.Empty;
        }

        if (tour.Id <= 0)
        {
            return NormalizeAssetUrl(tour.CoverImageUrl);
        }

        return NormalizeAssetUrl($"{BaseUrl.TrimEnd('/')}/api/tour/{tour.Id}/cover");
    }

    private string NormalizeAssetUrl(string? value)
    {
        var normalized = NormalizeUrl(value);
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return string.Empty;
        }

        var separator = normalized.Contains('?', StringComparison.Ordinal) ? "&" : "?";
        return $"{normalized}{separator}v={_assetVersion}";
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

    private void BumpAssetVersion()
    {
        _assetVersion = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
    }

    private static string GuessExtension(string normalizedUrl, string? contentType)
    {
        if (Uri.TryCreate(normalizedUrl, UriKind.Absolute, out var absolute))
        {
            var urlExtension = Path.GetExtension(absolute.AbsolutePath);
            if (!string.IsNullOrWhiteSpace(urlExtension))
            {
                return urlExtension;
            }
        }

        return contentType?.ToLowerInvariant() switch
        {
            "audio/wav" or "audio/x-wav" or "audio/wave" => ".wav",
            "audio/mpeg" => ".mp3",
            "audio/aac" => ".aac",
            "audio/mp4" => ".m4a",
            _ => ".bin"
        };
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string ReadVersionToken(Uri assetUri)
    {
        if (string.IsNullOrWhiteSpace(assetUri.Query))
        {
            return string.Empty;
        }

        var segments = assetUri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            var pair = segment.Split('=', 2);
            if (pair.Length == 2 && string.Equals(pair[0], "v", StringComparison.OrdinalIgnoreCase))
            {
                return Uri.UnescapeDataString(pair[1]);
            }
        }

        return string.Empty;
    }
}

public readonly record struct UrlProbeResult(
    string Url,
    int StatusCode,
    string ContentType,
    long? ContentLength);

public readonly record struct DownloadedFileResult(
    string Url,
    string LocalPath,
    string ContentType,
    long? ContentLength);
