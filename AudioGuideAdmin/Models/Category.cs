using System.ComponentModel.DataAnnotations;

namespace AudioGuideAdmin.Models;

public class Category
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Ma danh muc khong duoc de trong")]
    public string Slug { get; set; } = "";

    [Required(ErrorMessage = "Ten danh muc khong duoc de trong")]
    public string Name { get; set; } = "";

    public string Description { get; set; } = "";
    public string ThemeColor { get; set; } = "#17324d";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
