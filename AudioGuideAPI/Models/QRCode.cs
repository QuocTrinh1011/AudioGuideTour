namespace AudioGuideAPI.Models;

public class QRCode
{
    public int Id { get; set; }

    public int PoiId { get; set; }

    public string Code { get; set; } = string.Empty;
}