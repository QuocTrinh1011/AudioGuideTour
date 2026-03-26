namespace AudioGuideAPI.Models;

public class VisitHistory
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public int PoiId { get; set; }
    public string Language { get; set; } = "vi-VN";
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime EndTime { get; set; } = DateTime.UtcNow;
    public int Duration { get; set; }
    public string TriggerType { get; set; } = "manual";
    public string PlaybackMode { get; set; } = "tts";
    public bool WasAutoPlayed { get; set; } = true;
    public bool WasCompleted { get; set; } = true;
    public double ActivationDistanceMeters { get; set; }
}
