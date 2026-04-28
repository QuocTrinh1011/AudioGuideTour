using System.ComponentModel.DataAnnotations;

namespace AudioGuideOwnerPortal.Models;

public class LanguageOption
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Mã ngôn ngữ không được để trống")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Tên ngôn ngữ không được để trống")]
    public string Name { get; set; } = "";

    public string NativeName { get; set; } = "";
    public string Locale { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
