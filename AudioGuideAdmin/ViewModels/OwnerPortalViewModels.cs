using AudioGuideAdmin.Models;
using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.ViewModels;

public class OwnerLoginViewModel
{
    [Required(ErrorMessage = "Hãy nhập số điện thoại hoặc email")]
    public string Login { get; set; } = "";

    [Required(ErrorMessage = "Hãy nhập mật khẩu")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class OwnerRegisterViewModel
{
    [Required(ErrorMessage = "Họ tên không được để trống")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Số điện thoại không được để trống")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Email không được để trống")]
    [EmailAddress(ErrorMessage = "Email không hợp lệ")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Tên quán/cơ sở không được để trống")]
    public string BusinessName { get; set; } = "";

    [Required(ErrorMessage = "Mật khẩu không được để trống")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Hãy nhập lại mật khẩu")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Mật khẩu nhập lại chưa khớp")]
    public string ConfirmPassword { get; set; } = "";
}

public class OwnerPoiDashboardViewModel
{
    public ShopOwner Owner { get; set; } = new();
    public List<Poi> LivePois { get; set; } = new();
    public List<PoiSubmission> Submissions { get; set; } = new();
}
