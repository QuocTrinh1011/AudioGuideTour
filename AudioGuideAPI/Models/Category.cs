namespace AudioGuideAPI.Models;

public class Category
{
    public int Id { get; set; }
    public string Slug { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string ThemeColor { get; set; } = "#17324d";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
