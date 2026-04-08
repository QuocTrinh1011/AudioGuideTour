using AudioGuideAdmin.Data;
using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class PoiController : Controller
{
    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;

    public PoiController(AppDbContext context, ImageStorageOptions imageStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySuggestedImages()
    {
        var pois = await _context.Pois.ToListAsync();
        var updated = 0;
        var missing = 0;

        foreach (var poi in pois)
        {
            if (!string.IsNullOrWhiteSpace(poi.ImageUrl))
            {
                continue;
            }

            var suggestion = PoiImageSuggestionHelper.Suggest(poi);
            if (string.IsNullOrWhiteSpace(suggestion))
            {
                missing++;
                continue;
            }

            poi.ImageUrl = suggestion;
            poi.UpdatedAt = DateTime.UtcNow;
            updated++;
        }

        await _context.SaveChangesAsync();

        TempData["Success"] = updated > 0
            ? $"Da gan anh goi y cho {updated} POI."
            : "Khong co POI nao duoc gan anh goi y.";

        if (missing > 0)
        {
            TempData["Error"] = $"{missing} POI chua khop tu khoa de gan anh tu dong. Ban co the gan anh thu cong.";
        }

        return RedirectToAction(nameof(Index));
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
            poi.ImageUrl = await SaveImageAsync(poi.ImageFile, poi.ImageUrl);
            poi.AudioMode = string.IsNullOrWhiteSpace(poi.AudioUrl) ? "tts" : "tts-fallback";
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
        var poi = await _context.Pois
            .Include(x => x.Translations)
            .FirstOrDefaultAsync(x => x.Id == id);

        if (poi == null)
        {
            return NotFound();
        }

        ViewBag.Categories = BuildCategoryOptions(poi.Category);
        ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
        ViewBag.TranslationLinks = BuildTranslationLinks(poi);
        return View(poi);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ApplySuggestedImage(int id)
    {
        var poi = await _context.Pois.FindAsync(id);
        if (poi == null)
        {
            return NotFound();
        }

        var suggestion = PoiImageSuggestionHelper.Suggest(poi);
        if (string.IsNullOrWhiteSpace(suggestion))
        {
            TempData["Error"] = "Chua tim thay anh goi y phu hop cho POI nay. Ban hay gan anh thu cong.";
            return RedirectToAction(nameof(Edit), new { id });
        }

        poi.ImageUrl = suggestion;
        poi.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da gan anh goi y cho POI.";
        return RedirectToAction(nameof(Edit), new { id });
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
            ViewBag.TranslationLinks = BuildTranslationLinks(poi.Id);
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
            existing.ImageUrl = await SaveImageAsync(poi.ImageFile, poi.ImageUrl);
            existing.MapUrl = poi.MapUrl;
            existing.IsActive = poi.IsActive;
            existing.AudioMode = string.IsNullOrWhiteSpace(poi.AudioUrl) ? "tts" : "tts-fallback";
            existing.AudioUrl = poi.AudioUrl;
            existing.TtsScript = poi.TtsScript;
            existing.DefaultLanguage = poi.DefaultLanguage;
            existing.EstimatedDurationSeconds = poi.EstimatedDurationSeconds;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Da cap nhat POI thanh cong.";
            return RedirectToAction(nameof(Edit), new { id = existing.Id });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong cap nhat duoc POI. {ex.GetBaseException().Message}");
            ViewBag.Categories = BuildCategoryOptions(poi.Category);
            ViewBag.Languages = BuildLanguageOptions(poi.DefaultLanguage);
            ViewBag.TranslationLinks = BuildTranslationLinks(poi.Id);
            return View(poi);
        }
    }

    private async Task<string> SaveImageAsync(IFormFile? imageFile, string? currentUrl)
    {
        if (imageFile == null || imageFile.Length <= 0)
        {
            return (currentUrl ?? string.Empty).Trim();
        }

        Directory.CreateDirectory(_imageStorageOptions.RootPath);
        var extension = Path.GetExtension(imageFile.FileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            extension = ".png";
        }

        var fileName = $"{Guid.NewGuid():N}{extension}";
        var physicalPath = Path.Combine(_imageStorageOptions.RootPath, fileName);
        await using var stream = new FileStream(physicalPath, FileMode.Create);
        await imageFile.CopyToAsync(stream);
        return $"/images/{fileName}";
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

    private List<TranslationLanguageLinkViewModel> BuildTranslationLinks(Poi poi)
    {
        var existingLanguages = poi.Translations
            .Select(x => x.Language)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TranslationLanguageLinkViewModel
            {
                Code = x.Code,
                Name = x.Name,
                NativeName = string.IsNullOrWhiteSpace(x.NativeName) ? x.Name : x.NativeName,
                Exists = existingLanguages.Contains(x.Code),
                IsCurrent = false,
                Url = $"/Translation/EditForPoi?poiId={poi.Id}&language={x.Code}"
            })
            .ToList();
    }

    private List<TranslationLanguageLinkViewModel> BuildTranslationLinks(int poiId)
    {
        var existingLanguages = _context.PoiTranslations
            .Where(x => x.PoiId == poiId)
            .Select(x => x.Language)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new TranslationLanguageLinkViewModel
            {
                Code = x.Code,
                Name = x.Name,
                NativeName = string.IsNullOrWhiteSpace(x.NativeName) ? x.Name : x.NativeName,
                Exists = existingLanguages.Contains(x.Code),
                IsCurrent = false,
                Url = $"/Translation/EditForPoi?poiId={poiId}&language={x.Code}"
            })
            .ToList();
    }
}
