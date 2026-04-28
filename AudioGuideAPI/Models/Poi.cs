namespace AudioGuideAPI.Models;

public class Poi
{
    public int Id { get; set; }
    public string? OwnerId { get; set; }
    public string Name { get; set; } = "";
    public string Category { get; set; } = "food-street";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Radius { get; set; }
    public int ApproachRadiusMeters { get; set; } = 90;
    public int Priority { get; set; } = 1;
    public int DebounceSeconds { get; set; } = 15;
    public int CooldownSeconds { get; set; } = 120;
    public string TriggerMode { get; set; } = "both";
    public string ImageUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string AudioMode { get; set; } = "tts";
    public string AudioUrl { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string DefaultLanguage { get; set; } = "vi-VN";
    public int EstimatedDurationSeconds { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<PoiTranslation> Translations { get; set; } = new();
    public List<TourStop> TourStops { get; set; } = new();
}
