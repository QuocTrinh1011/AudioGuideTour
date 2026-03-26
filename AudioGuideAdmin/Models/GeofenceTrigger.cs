namespace AudioGuideAdmin.Models;

public class GeofenceTrigger
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int PoiId { get; set; }
    public string Language { get; set; } = "vi-VN";
    public string TriggerType { get; set; } = "enter";
    public string Status { get; set; } = "triggered";
    public double DistanceMeters { get; set; }
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
    public DateTime CooldownUntil { get; set; } = DateTime.UtcNow;
}
