namespace AudioGuideAPI.Models;

public class LanguageOption
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NativeName { get; set; } = "";
    public string Locale { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; } = 1;
}
