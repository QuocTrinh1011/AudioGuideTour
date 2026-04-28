using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Http;

namespace AudioGuideAdmin.Models;

public class Poi
{
    public int Id { get; set; }

    [MaxLength(64)]
    public string? OwnerId { get; set; }
    public ShopOwner? Owner { get; set; }

    [Required(ErrorMessage = "Tên không được để trống")]
    public string Name { get; set; } = "";

    public string Category { get; set; } = "food-street";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public List<PoiTranslation> Translations { get; set; } = new();

    [Required]
    public double Latitude { get; set; }

    [Required]
    public double Longitude { get; set; }

    public int Radius { get; set; }
    public int ApproachRadiusMeters { get; set; } = 90;
    public int Priority { get; set; } = 1;
    public int DebounceSeconds { get; set; } = 15;
    public int CooldownSeconds { get; set; } = 120;
    public string TriggerMode { get; set; } = "both";
    public string ImageUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public string AudioMode { get; set; } = "tts";
    public string AudioUrl { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string DefaultLanguage { get; set; } = "vi-VN";
    public int EstimatedDurationSeconds { get; set; } = 60;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public List<TourStop> TourStops { get; set; } = new();

    [NotMapped]
    public IFormFile? ImageFile { get; set; }
}
