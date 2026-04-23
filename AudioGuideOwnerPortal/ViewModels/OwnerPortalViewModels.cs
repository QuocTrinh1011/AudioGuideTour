using AudioGuideOwnerPortal.Models;
using System.ComponentModel.DataAnnotations;

namespace AudioGuideOwnerPortal.ViewModels;

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

public class OwnerDashboardViewModel
{
    public ShopOwner Owner { get; set; } = new();
    public int LivePoiCount { get; set; }
    public int ActivePoiCount { get; set; }
    public int DraftSubmissionCount { get; set; }
    public int PendingSubmissionCount { get; set; }
    public int ChangesRequestedCount { get; set; }
    public int ApprovedSubmissionCount { get; set; }
    public int TotalListenCount { get; set; }
    public int UniqueVisitorCount { get; set; }
    public double AverageListenSeconds { get; set; }
    public string TopLanguageLabel { get; set; } = "Chưa có dữ liệu";
    public int TopLanguageListenCount { get; set; }
    public string TopPoiName { get; set; } = "Chưa có dữ liệu";
    public int TopPoiListenCount { get; set; }
    public OwnerContentHealthViewModel ContentHealth { get; set; } = new();
    public List<OwnerTopPoiStatViewModel> TopPois { get; set; } = new();
    public List<OwnerLanguageUsageViewModel> LanguageUsage { get; set; } = new();
}

public class OwnerTopPoiStatViewModel
{
    public int PoiId { get; set; }
    public string PoiName { get; set; } = "";
    public string Category { get; set; } = "";
    public int ListenCount { get; set; }
    public double AverageListenSeconds { get; set; }
}

public class OwnerLanguageUsageViewModel
{
    public string Language { get; set; } = "vi-VN";
    public string Label { get; set; } = "";
    public int ListenCount { get; set; }
    public double Percentage { get; set; }
}

public class OwnerContentHealthViewModel
{
    public int MissingTranslationPoiCount { get; set; }
    public int MissingAudioFallbackPoiCount { get; set; }
    public int MissingImagePoiCount { get; set; }
    public int MissingDefaultTtsPoiCount { get; set; }
    public int SingleLanguagePoiCount { get; set; }
}

public class OwnerPoiTemplatePresetViewModel
{
    public string Key { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Category { get; set; } = "";
    public string Address { get; set; } = "";
}

public class OwnerPoiDetailsViewModel
{
    public ShopOwner Owner { get; set; } = new();
    public Poi Poi { get; set; } = new();
    public List<LanguageOption> Languages { get; set; } = new();

    public string GetLanguageLabel(string languageCode)
    {
        var language = Languages.FirstOrDefault(x => string.Equals(x.Code, languageCode, StringComparison.OrdinalIgnoreCase));
        if (language == null)
        {
            return languageCode;
        }

        return string.IsNullOrWhiteSpace(language.NativeName)
            ? $"{language.Name} ({language.Code})"
            : $"{language.Name} - {language.NativeName} ({language.Code})";
    }
}
