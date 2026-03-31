namespace AudioGuideAdmin.ViewModels;

public class TranslationLanguageLinkViewModel
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string NativeName { get; set; } = "";
    public bool Exists { get; set; }
    public bool IsCurrent { get; set; }
    public string Url { get; set; } = "";
}
