using System.Text.Json.Serialization;

namespace AudioTourApp.Models;

public class PoiItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string ImageUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public int Priority { get; set; }
    public int Radius { get; set; }
    public int CooldownSeconds { get; set; }
    public int DebounceSeconds { get; set; }
    [JsonPropertyName("distanceMeters")]
    public double DistanceMeters { get; set; }
    [JsonPropertyName("isNearest")]
    public bool IsNearest { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
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
    public int EstimatedDurationMinutes { get; set; }
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
