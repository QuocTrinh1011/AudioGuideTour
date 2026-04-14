namespace AudioGuideAdmin.ViewModels;

public class QrPublicPageViewModel
{
    public string Code { get; set; } = "";
    public string Note { get; set; } = "";
    public string Title { get; set; } = "";
    public string Summary { get; set; } = "";
    public string Description { get; set; } = "";
    public string Address { get; set; } = "";
    public string ImageUrl { get; set; } = "";
    public string AudioUrl { get; set; } = "";
    public string MapUrl { get; set; } = "";
    public string Language { get; set; } = "vi-VN";
    public string LanguageDisplayName { get; set; } = "Tiếng Việt";
    public string NarrationText { get; set; } = "";
    public string NarrationSource { get; set; } = "";
    public string DeepLinkUrl { get; set; } = "";
    public List<QrPublicLanguageOption> AvailableLanguages { get; set; } = new();
}

public class QrPublicLanguageOption
{
    public string Code { get; set; } = "";
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
    public bool IsSelected { get; set; }
}
