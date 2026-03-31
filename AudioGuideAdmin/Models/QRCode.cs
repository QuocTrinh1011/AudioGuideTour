namespace AudioGuideAdmin.Models;

public class QRCode
{
    public int Id { get; set; }
    public int PoiId { get; set; }
    public Poi? Poi { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
}
