namespace AudioGuideAPI.Models;

public class Poi
{
    public int Id { get; set; }

    public string Name { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public int Radius { get; set; }

    public string ImageUrl { get; set; }

    public bool IsActive { get; set; } = true;
    public string AudioUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}