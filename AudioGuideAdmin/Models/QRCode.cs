using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.Models;

public class QRCode
{
    public int Id { get; set; }

    [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn POI.")]
    public int PoiId { get; set; }

    public Poi? Poi { get; set; }

    [Required(ErrorMessage = "Mã QR không được để trống.")]
    public string Code { get; set; } = string.Empty;

    public string Note { get; set; } = string.Empty;
}
