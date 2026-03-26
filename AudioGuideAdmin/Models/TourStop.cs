namespace AudioGuideAdmin.Models;

public class TourStop
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public Tour? Tour { get; set; }
    public int PoiId { get; set; }
    public Poi? Poi { get; set; }
    public int SortOrder { get; set; }
    public bool AutoPlay { get; set; } = true;
    public string Note { get; set; } = "";
}
