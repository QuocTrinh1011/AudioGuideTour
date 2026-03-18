namespace AudioGuideAdmin.Models;

public class VisitHistory
{
    public int Id { get; set; }

    public string UserId { get; set; } = "";

    public int PoiId { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Duration { get; set; }
}