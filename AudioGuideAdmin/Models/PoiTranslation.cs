using AudioGuideAdmin.Models;
namespace AudioGuideAdmin.Models;

public class PoiTranslation
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public Poi? Poi { get; set; } 

    public string Language { get; set; } = "";

    public string Title { get; set; } = "";

    public string Description { get; set; } = "";

    public string AudioUrl { get; set; } = "";
}