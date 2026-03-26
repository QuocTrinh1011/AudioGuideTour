namespace AudioGuideAPI.Models;

public class UserTracking
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Accuracy { get; set; }
    public double? SpeedMetersPerSecond { get; set; }
    public double? Bearing { get; set; }
    public string Source { get; set; } = "gps";
    public bool IsForeground { get; set; } = true;
    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}
