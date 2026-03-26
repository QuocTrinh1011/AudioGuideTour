using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.Models;

public class LanguageOption
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ma ngon ngu khong duoc de trong")]
    public string Code { get; set; } = "";

    [Required(ErrorMessage = "Ten ngon ngu khong duoc de trong")]
    public string Name { get; set; } = "";

    public string NativeName { get; set; } = "";
    public string Locale { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
