using AudioGuideAdmin.Models;
using System.ComponentModel.DataAnnotations;
namespace AudioGuideAdmin.Models;
public class Poi
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tên không được để trống")]
    public string Name { get; set; } = "";

    public List<PoiTranslation>? Translations { get; set; }

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public int Radius { get; set; }

    public string ImageUrl { get; set; } = "";

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}