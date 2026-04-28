namespace AudioGuideAPI.Models;

public class Tour
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Language { get; set; } = "vi-VN";
    public string? CoverImageUrl { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int EstimatedDurationMinutes { get; set; } = 45;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TourTranslation> Translations { get; set; } = new();
    public List<TourStop> Stops { get; set; } = new();
}
