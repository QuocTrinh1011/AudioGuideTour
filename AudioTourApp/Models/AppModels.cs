using System.Text.Json.Serialization;

namespace AudioTourApp.Models;

public class PoiItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string AudioMode { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string ImageUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public string TriggerMode { get; set; } = "both";
    public int Priority { get; set; }
    public int Radius { get; set; }
    public int ApproachRadiusMeters { get; set; }
    public int CooldownSeconds { get; set; }
    public int DebounceSeconds { get; set; }
    public int EstimatedDurationSeconds { get; set; }
    [JsonPropertyName("distanceMeters")]
    public double DistanceMeters { get; set; }
    [JsonPropertyName("isNearest")]
    public bool IsNearest { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class LanguageItem
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string Locale { get; set; } = "";
    public override string ToString() => string.IsNullOrWhiteSpace(NativeName) ? Code : $"{NativeName} ({Code})";
}

public class CategoryItem
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ThemeColor { get; set; } = "#17324d";
}

public class GeofenceCheckResponse
{
    public bool ShouldPlay { get; set; }
    public string Reason { get; set; } = "";
    public DateTime? NextEligibleAt { get; set; }
    public PoiItem? TriggeredPoi { get; set; }
    public List<PoiItem> NearbyPois { get; set; } = new();
}

public class BootstrapResponse
{
    public List<LanguageItem> Languages { get; set; } = new();
    public List<CategoryItem> Categories { get; set; } = new();
    public List<PoiItem> Pois { get; set; } = new();
    public List<TourItem> Tours { get; set; } = new();
    public string RequestedLanguage { get; set; } = "vi-VN";
}

public class TourItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string CoverImageUrl { get; set; } = "";
    public int EstimatedDurationMinutes { get; set; }
    public List<TourStopItem> Stops { get; set; } = new();
    public int StopCount => Stops?.Count ?? 0;
}

public class TourStopItem
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public int PoiId { get; set; }
    public int SortOrder { get; set; }
    public bool AutoPlay { get; set; } = true;
    public string Note { get; set; } = "";
    public PoiItem? Poi { get; set; }
}

public class QrLookupResponse
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Note { get; set; } = "";
    public PoiItem? Poi { get; set; }
}

public class QrLookupHistoryItem
{
    public string Code { get; set; } = "";
    public string PoiTitle { get; set; } = "";
    public string PoiSummary { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string ImageUrl { get; set; } = "";
    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
}

public class AudioPlaybackRequest
{
    public PoiItem Poi { get; set; } = new();
    public string UserId { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string TriggerType { get; set; } = "manual";
    public bool WasAutoPlayed { get; set; }
}

public class VisitHistoryRequest
{
    public string UserId { get; set; } = "";
    public int PoiId { get; set; }
    public string Language { get; set; } = "vi-VN";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; }
    public string TriggerType { get; set; } = "manual";
    public string PlaybackMode { get; set; } = "tts";
    public bool WasAutoPlayed { get; set; }
    public bool WasCompleted { get; set; } = true;
    public double ActivationDistanceMeters { get; set; }
}

public class LocationUpdateRequest
{
    public string UserId { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public double? Bearing { get; set; }
    public bool IsForeground { get; set; } = true;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}

public class VisitorProfile
{
    public string Id { get; set; } = "";
    public string DeviceId { get; set; } = "";
    public string DisplayName { get; set; } = "Khách ẩn danh";
    public string Language { get; set; } = "vi-VN";
    public bool AllowBackgroundTracking { get; set; } = true;
    public bool AllowAutoPlay { get; set; } = true;
    public DateTime CreatedAt { get; set; }
    public DateTime LastSeenAt { get; set; }
}
