using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
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

    public IActionResult Create(int? poiId = null, string? language = null, bool contextLocked = false)
    {
        var model = new PoiTranslation();
        if (poiId.HasValue && poiId.Value > 0)
        {
            model.PoiId = poiId.Value;
        }

        if (!string.IsNullOrWhiteSpace(language))
        {
            model.Language = language;
        }

        PrepareTranslationForm(model.PoiId, model.Language, contextLocked);
        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PoiTranslation model, bool contextLocked = false)
    {
        if (model.PoiId <= 0)
        {
            ModelState.AddModelError(nameof(model.PoiId), "Vui lòng chọn POI trước khi tạo bản dịch.");
        }

        if (string.IsNullOrWhiteSpace(model.Language))
        {
            ModelState.AddModelError(nameof(model.Language), "Vui lòng chọn ngôn ngữ.");
        }

        var exists = await _context.PoiTranslations
            .AnyAsync(x => x.PoiId == model.PoiId && x.Language == model.Language);

        if (exists)
        {
            ModelState.AddModelError("", "Ngôn ngữ này đã tồn tại cho POI này.");
        }

        if (!_context.LanguageOptions.Any(x => x.Code == model.Language && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.Language), "Chỉ được chọn ngôn ngữ trong danh sách đang hoạt động.");
        }

        if (!ModelState.IsValid)
        {
            PrepareTranslationForm(model.PoiId, model.Language, contextLocked);
            return View(model);
        }

        try
        {
            model.Id = 0;
            model.UpdatedAt = DateTime.UtcNow;
            _context.PoiTranslations.Add(model);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã thêm bản dịch thành công.";
            if (contextLocked)
            {
                return RedirectToAction(nameof(EditForPoi), new { poiId = model.PoiId, language = model.Language });
            }

            return RedirectToAction(nameof(EditForPoi), new { poiId = model.PoiId, language = model.Language });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Không thêm được bản dịch. {ex.GetBaseException().Message}");
            PrepareTranslationForm(model.PoiId, model.Language, contextLocked);
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

        PrepareTranslationForm(item.PoiId, item.Language, false);
        return View(item);
    }

    public async Task<IActionResult> EditForPoi(int poiId, string? language = null)
    {
        var poi = await _context.Pois.FindAsync(poiId);
        if (poi == null)
        {
            return NotFound();
        }

        var selectedLanguage = string.IsNullOrWhiteSpace(language)
            ? poi.DefaultLanguage
            : language;

        var item = await _context.PoiTranslations
            .FirstOrDefaultAsync(x => x.PoiId == poiId && x.Language == selectedLanguage);

        if (item != null)
        {
            PrepareTranslationForm(item.PoiId, item.Language, true);
            return View("Edit", item);
        }

        var model = new PoiTranslation
        {
            PoiId = poiId,
            Language = selectedLanguage ?? "vi-VN",
            Title = poi.Name,
            Summary = poi.Summary,
            Description = poi.Description,
            TtsScript = poi.TtsScript
        };

        PrepareTranslationForm(model.PoiId, model.Language, true);
        return View("Create", model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(PoiTranslation model, bool contextLocked = false)
    {
        var duplicate = await _context.PoiTranslations
            .AnyAsync(x => x.Id != model.Id && x.PoiId == model.PoiId && x.Language == model.Language);
        if (duplicate)
        {
            ModelState.AddModelError(nameof(model.Language), "POI này đã có bản dịch cho ngôn ngữ được chọn.");
        }

        if (!_context.LanguageOptions.Any(x => x.Code == model.Language && x.IsActive))
        {
            ModelState.AddModelError(nameof(model.Language), "Chỉ được chọn ngôn ngữ trong danh sách đang hoạt động.");
        }

        if (!ModelState.IsValid)
        {
            PrepareTranslationForm(model.PoiId, model.Language, contextLocked);
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
            existing.AudioUrl = model.AudioUrl;
            existing.TtsScript = model.TtsScript;
            existing.VoiceName = model.VoiceName;
            existing.IsPublished = model.IsPublished;
            existing.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã cấp nhat ban dich thanh cong.";
            if (contextLocked)
            {
                return RedirectToAction(nameof(EditForPoi), new { poiId = existing.PoiId, language = existing.Language });
            }

            return RedirectToAction(nameof(EditForPoi), new { poiId = existing.PoiId, language = existing.Language });
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, $"Không cập nhật được bản dịch. {ex.GetBaseException().Message}");
            PrepareTranslationForm(model.PoiId, model.Language, contextLocked);
            return View(model);
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await _context.PoiTranslations.FindAsync(id);
        if (item != null)
        {
            _context.PoiTranslations.Remove(item);
            await _context.SaveChangesAsync();
            TempData["Success"] = "Đã xóa bản dịch.";
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

    private void PrepareTranslationForm(int? poiId = null, string? language = null, bool contextLocked = false)
    {
        ViewBag.Pois = BuildPoiOptions(poiId, contextLocked);
        ViewBag.Languages = BuildLanguageOptions(language);
        ViewBag.TranslationLinks = contextLocked && poiId.HasValue && poiId.Value > 0
            ? BuildTranslationLinks(poiId, language)
            : new List<TranslationLanguageLinkViewModel>();
        ViewBag.ContextLocked = contextLocked;
        ViewBag.ContextPoiName = poiId.HasValue
            ? _context.Pois.Where(x => x.Id == poiId.Value).Select(x => x.Name).FirstOrDefault()
            : null;
    }

    private List<SelectListItem> BuildPoiOptions(int? selectedPoiId, bool contextLocked)
    {
        var items = new List<SelectListItem>();

        if (!contextLocked)
        {
            items.Add(new SelectListItem("Chon POI", "", !selectedPoiId.HasValue || selectedPoiId.Value <= 0));
        }

        items.AddRange(_context.Pois
            .OrderBy(x => x.Name)
            .Select(x => new SelectListItem(x.Name, x.Id.ToString(), selectedPoiId.HasValue && x.Id == selectedPoiId.Value))
            .ToList());

        return items;
    }

    private List<TranslationLanguageLinkViewModel> BuildTranslationLinks(int? poiId, string? currentLanguage)
    {
        if (!poiId.HasValue || poiId.Value <= 0)
        {
            return new List<TranslationLanguageLinkViewModel>();
        }

        var existingLanguages = _context.PoiTranslations
            .Where(x => x.PoiId == poiId.Value)
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
                IsCurrent = x.Code == currentLanguage,
                Url = $"/Translation/EditForPoi?poiId={poiId.Value}&language={x.Code}"
            })
            .ToList();
    }
}
