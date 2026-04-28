using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.Models;

public class ShopOwner
{
    [MaxLength(64)]
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    [Required(ErrorMessage = "Họ tên không được để trống")]
    [MaxLength(200)]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    [MaxLength(50)]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    [MaxLength(150)]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Tên quán/cơ sở không được để trống")]
    [MaxLength(200)]
    public string BusinessName { get; set; } = "";

    [MaxLength(200)]
    public string PasswordHash { get; set; } = "";

    [MaxLength(200)]
    public string PasswordSalt { get; set; } = "";

    [MaxLength(40)]
    public string Status { get; set; } = ShopOwnerStatus.Pending;

    [MaxLength(1000)]
    public string AdminNote { get; set; } = "";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ApprovedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public int? ApprovedByAdminId { get; set; }

    public List<PoiSubmission> Submissions { get; set; } = new();
    public List<Poi> Pois { get; set; } = new();
}
