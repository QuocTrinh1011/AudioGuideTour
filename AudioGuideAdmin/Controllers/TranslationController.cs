using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers;

public class TranslationController : Controller
{
    private readonly AppDbContext _context;

    public TranslationController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var data = await _context.PoiTranslations
            .Include(x => x.Poi)
            .OrderBy(x => x.Poi!.Name)
            .ThenBy(x => x.Language)
            .ToListAsync();

        return View(data);
    }

    public IActionResult Create()
    {
        ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name");
        ViewBag.Languages = BuildLanguageOptions();
        return View(new PoiTranslation());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiTranslation model)
    {
        var exists = await _context.PoiTranslations
            .AnyAsync(x => x.PoiId == model.PoiId && x.Language == model.Language);

        if (exists)
        {
            ModelState.AddModelError("", "Ngon ngu nay da ton tai cho POI nay.");
        }

        if (!_context.LanguageOptions.Any(x => x.Code == model.Language && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.Language), "Chi duoc chon ngon ngu trong danh sach dang hoat dong.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name", model.PoiId);
            ViewBag.Languages = BuildLanguageOptions(model.Language);
            return View(model);
        }

        try
        {
            model.AudioUrl = string.Empty;
            model.UpdatedAt = DateTime.UtcNow;
            _context.PoiTranslations.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da them ban dich thanh cong.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong them duoc ban dich. {ex.GetBaseException().Message}");
            ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name", model.PoiId);
            ViewBag.Languages = BuildLanguageOptions(model.Language);
            return View(model);
        }
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.PoiTranslations.FindAsync(id);
        if (item == null)
        {
            return NotFound();
        }

        ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name", item.PoiId);
        ViewBag.Languages = BuildLanguageOptions(item.Language);
        return View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiTranslation model)
    {
        if (!_context.LanguageOptions.Any(x => x.Code == model.Language && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.Language), "Chi duoc chon ngon ngu trong danh sach dang hoat dong.");
        }

        if (!ModelState.IsValid)
        {
            ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name", model.PoiId);
            ViewBag.Languages = BuildLanguageOptions(model.Language);
            return View(model);
        }

        var existing = await _context.PoiTranslations.FindAsync(model.Id);
        if (existing == null)
        {
            return NotFound();
        }

        try
        {
            existing.PoiId = model.PoiId;
            existing.Language = model.Language;
            existing.Title = model.Title;
            existing.Summary = model.Summary;
            existing.Description = model.Description;
            existing.AudioUrl = string.Empty;
            existing.TtsScript = model.TtsScript;
            existing.VoiceName = model.VoiceName;
            existing.IsPublished = model.IsPublished;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Da cap nhat ban dich thanh cong.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Khong cap nhat duoc ban dich. {ex.GetBaseException().Message}");
            ViewBag.Pois = new SelectList(_context.Pois.OrderBy(x => x.Name), "Id", "Name", model.PoiId);
            ViewBag.Languages = BuildLanguageOptions(model.Language);
            return View(model);
        }
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.PoiTranslations.FindAsync(id);
        if (item != null)
        {
            _context.PoiTranslations.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Da xoa ban dich.";
        }

        return RedirectToAction(nameof(Index));
    }

    private List<SelectListItem> BuildLanguageOptions(string? selected = null)
    {
        return _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select(x => new SelectListItem($"{x.NativeName} ({x.Code})", x.Code, x.Code == selected))
            .ToList();
    }
}
