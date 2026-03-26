namespace AudioGuideAPI.DTOs;

public class LocationRequest
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
