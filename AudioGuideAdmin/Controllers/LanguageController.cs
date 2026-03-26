using AudioGuideAdmin.Data;
using AudioGuideAdmin.Models;
using Microsoft.AspNetCore.Mvc;

namespace AudioGuideAdmin.Controllers;

public class LanguageController : Controller
{
    private readonly AppDbContext _context;

    public LanguageController(AppDbContext context)
    {
        _context = context;
    }

    public IActionResult Index()
    {
        var items = _context.LanguageOptions
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return View(items);
    }

    public IActionResult Create()
    {
        return View(new LanguageOption());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LanguageOption model)
    {
        Normalize(model);

        if (_context.LanguageOptions.Any(x => x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Ma ngon ngu da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        _context.LanguageOptions.Add(model);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da them ngon ngu moi.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var item = await _context.LanguageOptions.FindAsync(id);
        return item == null ? NotFound() : View(item);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(LanguageOption model)
    {
        Normalize(model);

        if (_context.LanguageOptions.Any(x => x.Id != model.Id && x.Code == model.Code))
        {
            ModelState.AddModelError(nameof(model.Code), "Ma ngon ngu da ton tai.");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        var existing = await _context.LanguageOptions.FindAsync(model.Id);
        if (existing == null)
        {
            return NotFound();
        }

        existing.Code = model.Code;
        existing.Name = model.Name;
        existing.NativeName = model.NativeName;
        existing.Locale = model.Locale;
        existing.SortOrder = model.SortOrder;
        existing.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        TempData["Success"] = "Da cap nhat ngon ngu.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.LanguageOptions.FindAsync(id);
        if (item == null)
        {
            return RedirectToAction(nameof(Index));
        }

        var usedByTranslation = _context.PoiTranslations.Any(x => x.Language == item.Code);
        var usedByPoi = _context.Pois.Any(x => x.DefaultLanguage == item.Code);

        if (usedByTranslation || usedByPoi)
        {
            TempData["Error"] = "Khong the xoa ngon ngu dang duoc su dung trong POI hoac translation.";
            return RedirectToAction(nameof(Index));
        }

        _context.LanguageOptions.Remove(item);
        await _context.SaveChangesAsync();
        TempData["Success"] = "Da xoa ngon ngu.";
        return RedirectToAction(nameof(Index));
    }

    private static void Normalize(LanguageOption model)
    {
        model.Code = model.Code.Trim();
        model.Name = model.Name.Trim();
        model.NativeName = model.NativeName?.Trim() ?? "";
        model.Locale = string.IsNullOrWhiteSpace(model.Locale) ? model.Code : model.Locale.Trim();
    }
}
