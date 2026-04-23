using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AudioGuideAdmin.ViewModels;

public class TourFormViewModel
{
    public Tour Tour { get; set; } = new();
    public string StopPoiIds { get; set; } = "";
    public string StopConfigJson { get; set; } = "[]";
    public List<SelectListItem> PoiOptions { get; set; } = new();
    public List<SelectListItem> LanguageOptions { get; set; } = new();
    public List<TourTranslationInputViewModel> Translations { get; set; } = new();
    public IFormFile? CoverImageFile { get; set; }
}

public class TourTranslationInputViewModel
{
    public string Language { get; set; } = "vi-VN";
    public string LanguageLabel { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
}
