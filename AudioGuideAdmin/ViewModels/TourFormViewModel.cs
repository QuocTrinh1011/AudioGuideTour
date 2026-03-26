using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AudioGuideAdmin.ViewModels;

public class TourFormViewModel
{
    public Tour Tour { get; set; } = new();
    public string StopPoiIds { get; set; } = "";
    public List<SelectListItem> PoiOptions { get; set; } = new();
}
