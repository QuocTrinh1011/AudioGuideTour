namespace AudioGuideAPI.Models;

public class User
{
    public string Id { get; set; }

    public string DeviceId { get; set; }

    public string Language { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}