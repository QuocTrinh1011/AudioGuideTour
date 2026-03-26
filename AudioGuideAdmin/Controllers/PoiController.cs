using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class PoiController : Controller
{
    private readonly AppDbContext _context;

    public PoiController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index(string? search, bool? activeOnly)
    {
        var pois = _context.Pois.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            pois = pois.Where(p => p.Name.Contains(search) || p.Category.Contains(search) || p.Address.Contains(search));
        }

        if (activeOnly == true)
        {
            pois = pois.Where(p => p.IsActive);
        }

        return View(pois
            .OrderByDescending(x => x.Priority)
            .ThenBy(x => x.Name)
            .ToList());
    }

    public IActionResult Create()
    {
        ViewBag.Categories = BuildCategoryOptions();
        ViewBag.Languages = BuildLanguageOptions();
        return View(new Poi());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Poi poi)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = BuildCategoryOptions(poi.Category);
            ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
            return View(poi);
        }

        try
        {
            poi.AudioMode = "tts";
            poi.AudioUrl = string.Empty;
            poi.CreatedAt = DateTime.UtcNow;
            poi.UpdatedAt = DateTime.UtcNow;
            _context.Pois.Add(poi);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da tao POI thanh cong.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong luu duoc POI. {ex.GetBaseException().Message}");
            ViewBag.Categories = BuildCategoryOptions(poi.Category);
            ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
            return View(poi);
        }
    }

    public async Task<IActionResult> Details(int id)
    {
        var poi = await _context.Pois
            .Include(p => p.Translations)
            .Include(p => p.TourStops)
            .ThenInclude(x => x.Tour)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        return View(poi);
    }

    public async Task<IActionResult> Delete(int id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi != null)
        {
            _context.Pois.Remove(poi);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da xoa POI.";
        }

        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null)
        {
            return NotFound();
        }

        ViewBag.Categories = BuildCategoryOptions(poi.Category);
        ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
        return View(poi);
    }

    public IActionResult Map()
    {
        var pois = _context.Pois
            .Where(x => x.IsActive)
            .OrderByDescending(x => x.Priority)
            .ToList();
        return View(pois);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(Poi poi)
    {
        if (!ModelState.IsValid)
        {
            ViewBag.Categories = BuildCategoryOptions(poi.Category);
            ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
            return View(poi);
        }

        var existing = await _context.Pois.FindAsync(poi.Id);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            existing.Name = poi.Name;
            existing.Category = poi.Category;
            existing.Summary = poi.Summary;
            existing.Description = poi.Description;
            existing.Address = poi.Address;
            existing.Latitude = poi.Latitude;
            existing.Longitude = poi.Longitude;
            existing.Radius = poi.Radius;
            existing.ApproachRadiusMeters = poi.ApproachRadiusMeters;
            existing.Priority = poi.Priority;
            existing.DebounceSeconds = poi.DebounceSeconds;
            existing.CooldownSeconds = poi.CooldownSeconds;
            existing.TriggerMode = poi.TriggerMode;
            existing.ImageUrl = poi.ImageUrl;
            existing.MapUrl = poi.MapUrl;
            existing.IsActive = poi.IsActive;
            existing.AudioMode = "tts";
            existing.AudioUrl = string.Empty;
            existing.TtsScript = poi.TtsScript;
            existing.DefaultLanguage = poi.DefaultLanguage;
            existing.EstimatedDurationSeconds = poi.EstimatedDurationSeconds;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Da cap nhat POI thanh cong.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong cap nhat duoc POI. {ex.GetBaseException().Message}");
            ViewBag.Categories = BuildCategoryOptions(poi.Category);
            ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
            return View(poi);
        }
    }

    private List<SelectListItem> BuildCategoryOptions(string? selected = null)
    {
        return _context.Categories
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Slug, x.Slug == selected))
            .ToList();
    }

    private List<SelectListItem> BuildLanguageOptions(string? selected = null)
    {
        return _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.Name} ({x.Code})", x.Code, x.Code == selected))
            .ToList();
    }
}
