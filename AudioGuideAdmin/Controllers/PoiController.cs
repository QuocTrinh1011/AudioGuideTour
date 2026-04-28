using AudioGuideAdmin.Controllers.Data;
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
        TempData["Error"] = "Admin không còn quản lý POI trực tiếp ở màn này. Hãy dùng mục Chủ quán và Duyệt POI.";
        return RedirectToAction("Index", "PoiReview");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplySuggestedImages()
    {
        TempData["Error"] = "Ảnh POI giờ nên được gửi qua submission của chủ quán và chờ admin duyệt.";
        return RedirectToAction("Index", "PoiReview");
    }

    public IActionResult Create()
    {
        TempData["Error"] = "Admin không tạo POI trực tiếp nữa. Chủ quán cần gửi submission để admin duyệt.";
        return RedirectToAction("Index", "PoiReview");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Create(Poi poi)
    {
        TempData["Error"] = "Admin không tạo POI trực tiếp nữa. Hãy duyệt submission từ chủ quán.";
        return RedirectToAction("Index", "PoiReview");
    }

    public IActionResult Details(int id)
    {
        TempData["Error"] = "Admin không còn xem/sửa POI trực tiếp ở màn này. Hãy mở submission hoặc dùng các màn read-only khác.";
        return RedirectToAction("Index", "PoiReview");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Delete(int id)
    {
        TempData["Error"] = "Admin không xóa POI live trực tiếp ở màn cũ nữa.";
        return RedirectToAction("Index", "PoiReview");
    }

    public IActionResult Edit(int id)
    {
        TempData["Error"] = "Admin không sửa POI trực tiếp nữa. Hãy review submission của chủ quán.";
        return RedirectToAction("Index", "PoiReview");
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ApplySuggestedImage(int id)
    {
        TempData["Error"] = "Admin không gắn ảnh trực tiếp cho POI live nữa. Hãy duyệt submission có ảnh từ chủ quán.";
        return RedirectToAction("Index", "PoiReview");
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
    public IActionResult Edit(Poi poi)
    {
        TempData["Error"] = "Admin không cập nhật POI live trực tiếp nữa. Hãy duyệt submission từ chủ quán.";
        return RedirectToAction("Index", "PoiReview");
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

    private void ValidateNarrationMode(Poi poi)
    {
        poi.AudioMode = NormalizeAudioMode(poi.AudioMode, poi.AudioUrl);

        if ((string.Equals(poi.AudioMode, "audio", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(poi.AudioMode, "audio-priority", StringComparison.OrdinalIgnoreCase)) &&
            string.IsNullOrWhiteSpace(poi.AudioUrl))
        {
            ModelState.AddModelError(nameof(poi.AudioUrl), "Chế độ audio cần có file audio URL.");
        }
    }

    private static string NormalizeAudioMode(string? requestedMode, string? audioUrl)
    {
        var normalized = requestedMode?.Trim().ToLowerInvariant();
        return normalized switch
        {
            "audio" => "audio",
            "audio-priority" => "audio-priority",
            "tts-fallback" => string.IsNullOrWhiteSpace(audioUrl) ? "tts" : "tts-fallback",
            "tts" => "tts",
            _ => string.IsNullOrWhiteSpace(audioUrl) ? "tts" : "tts-fallback"
        };
    }
}
