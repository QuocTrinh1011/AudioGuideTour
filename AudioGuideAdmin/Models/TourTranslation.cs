namespace AudioGuideAdmin.Models;

public class TourTranslation
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public Tour? Tour { get; set; }
    public string Language { get; set; } = "vi-VN";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
