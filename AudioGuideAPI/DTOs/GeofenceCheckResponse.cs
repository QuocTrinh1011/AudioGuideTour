namespace AudioGuideAPI.DTOs;

public class GeofenceCheckResponse
{
    public bool ShouldPlay { get; set; }
    public string Reason { get; set; } = "";
    public DateTime? NextEligibleAt { get; set; }
    public GeofencePoiResponse? TriggeredPoi { get; set; }
    public List<GeofencePoiResponse> NearbyPois { get; set; } = new();
}

public class GeofencePoiResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Title { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public double DistanceMeters { get; set; }
    public int Priority { get; set; }
    public int Radius { get; set; }
    public int CooldownSeconds { get; set; }
    public int DebounceSeconds { get; set; }
}
