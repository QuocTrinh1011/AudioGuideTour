namespace AudioTourApp.Models;

public class POI
{
    public int Id { get; set; }

    public string Name { get; set; }

    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public double RadiusMeters { get; set; }

    public int Priority { get; set; }

    public string Description { get; set; }

    public string AudioUrl { get; set; } // để phát audio sau này
}