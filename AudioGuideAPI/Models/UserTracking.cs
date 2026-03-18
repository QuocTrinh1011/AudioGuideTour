namespace AudioGuideAPI.Models;

public class UserTracking
{
    public int Id { get; set; }

    public string UserId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double Accuracy { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.Now;
}