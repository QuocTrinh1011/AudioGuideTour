using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AudioGuideAdmin.Models;

public class PoiTranslationSubmission
{
    public int Id { get; set; }

    [Required]
    [MaxLength(64)]
    public string SubmissionId { get; set; } = "";
    public PoiSubmission? Submission { get; set; }

    [Required]
    [MaxLength(20)]
    public string Language { get; set; } = "vi-VN";

    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string TtsScript { get; set; } = "";
    public string VoiceName { get; set; } = "";
    public int SortOrder { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    [NotMapped]
    public IFormFile? AudioFile { get; set; }
}
