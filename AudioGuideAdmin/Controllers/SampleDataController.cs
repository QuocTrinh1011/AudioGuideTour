using AudioGuideAdmin.Data;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class SampleDataController : Controller
{
    private readonly AppDbContext _context;

    public SampleDataController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var model = new SampleDataSummaryViewModel
        {
            CategoryCount = _context.Categories.Count(),
            PoiCount = _context.Pois.Count(),
            TranslationCount = _context.PoiTranslations.Count(),
            TourCount = _context.Tours.Count(),
            TrackingCount = _context.UserTrackings.Count(),
            VisitCount = _context.VisitHistories.Count(),
            TriggerCount = _context.GeofenceTriggers.Count()
        };

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Seed()
    {
        await AppDataInitializer.SeedAllDemoDataAsync(_context);
        TempData["Success"] = "Da nap bo du lieu mau cho toan bo web admin.";
        return RedirectToAction(nameof(Index));
    }
}

public class SampleDataSummaryViewModel
{
    public int CategoryCount { get; set; }
    public int PoiCount { get; set; }
    public int TranslationCount { get; set; }
    public int TourCount { get; set; }
    public int TrackingCount { get; set; }
    public int VisitCount { get; set; }
    public int TriggerCount { get; set; }
}
