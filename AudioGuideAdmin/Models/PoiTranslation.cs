namespace AudioGuideAdmin.Models;

public class PoiTranslation
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public Poi? Poi { get; set; }
    public string Language { get; set; } = "vi-VN";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public bool IsPublished { get; set; } = true;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
