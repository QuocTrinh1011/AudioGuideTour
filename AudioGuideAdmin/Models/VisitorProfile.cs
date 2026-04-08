namespace AudioGuideAdmin.Models;

public class VisitorProfile
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string DeviceId { get; set; } = Guid.NewGuid().ToString("N");
    public string DisplayName { get; set; } = "Khách ẩn danh";
    public string Language { get; set; } = "vi-VN";
    public bool AllowBackgroundTracking { get; set; } = true;
    public bool AllowAutoPlay { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;
}
