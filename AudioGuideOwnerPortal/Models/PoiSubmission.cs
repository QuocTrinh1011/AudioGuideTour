using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioGuideOwnerPortal.Models;

public class PoiSubmission
{
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    public int? PoiId { get; set; }
    public Poi? Poi { get; set; }

    [Required]
    [MaxLength(64)]
    public string OwnerId { get; set; } = "";
    public ShopOwner? Owner { get; set; }
    public List<PoiTranslationSubmission> TranslationSubmissions { get; set; } = new();

    [MaxLength(20)]
    public string SubmissionType { get; set; } = "create";

    [MaxLength(40)]
    public string Status { get; set; } = PoiSubmissionStatus.Draft;

    [MaxLength(1000)]
    public string ReviewNote { get; set; } = "";

    [Required(ErrorMessage = "Tên POI không được để trống")]
    [MaxLength(200)]
    public string Name { get; set; } = "";

    [MaxLength(100)]
    public string Category { get; set; } = "food-street";

    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";

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
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public int? ReviewedByAdminId { get; set; }

    [NotMapped]
    public IFormFile? ImageFile { get; set; }
}
