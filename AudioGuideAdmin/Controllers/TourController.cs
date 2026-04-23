using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Helpers;
using AudioGuideAdmin.Models;
using AudioGuideAdmin.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace AudioGuideAdmin.Controllers;

public class TourController : Controller
{
    private readonly AppDbContext _context;
    private readonly ImageStorageOptions _imageStorageOptions;

    public TourController(AppDbContext context, ImageStorageOptions imageStorageOptions)
    {
        _context = context;
        _imageStorageOptions = imageStorageOptions;
    }

    public IActionResult Index()
    {
        var tours = _context.Tours
            .Include(x => x.Translations)
            .Include(x => x.Stops)
            .OrderBy(x => x.Name)
            .ToList();

        return View(tours);
    }

    public IActionResult Create()
    {
        return View(BuildForm(new Tour()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TourFormViewModel model)
    {
        var stopConfigs = ParseStopConfigs(model.StopConfigJson, model.StopPoiIds);
        var translationInputs = NormalizeTourTranslationInputs(model.Translations, model.Tour.Language, model.Tour.Name, model.Tour.Description);
        model.StopPoiIds = string.Join(",", stopConfigs.Select(x => x.PoiId));
        model.StopConfigJson = SerializeStopConfigs(stopConfigs);
        ModelState.Remove(nameof(model.StopPoiIds));
        ModelState.Remove(nameof(model.StopConfigJson));
        if (string.IsNullOrWhiteSpace(model.StopPoiIds))
        {
            ModelState.AddModelError(nameof(model.StopPoiIds), "Hãy chọn ít nhất một POI cho tour.");
            ModelState.AddModelError(string.Empty, "Tour chưa có điểm dừng nào. Hãy thêm ít nhất một POI vào danh sách điểm dừng trước khi lưu.");
        }

        if (!ModelState.IsValid)
        {
            return View(BuildForm(model.Tour, model.StopPoiIds, model.StopConfigJson, translationInputs));
        }

        model.Tour.CoverImageUrl = await SaveImageAsync(model.CoverImageFile, model.Tour.CoverImageUrl);
        model.Tour.CreatedAt = DateTime.UtcNow;
        model.Tour.UpdatedAt = DateTime.UtcNow;
        model.Tour.Stops = BuildStops(stopConfigs, model.Tour.Id);
        model.Tour.Translations = BuildTranslations(translationInputs, model.Tour.Language, model.Tour.Id);

        _context.Tours.Add(model.Tour);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Edit(int id)
    {
        var tour = await _context.Tours
            .Include(x => x.Translations)
            .Include(x => x.Stops.OrderBy(s => s.SortOrder))
            .FirstOrDefaultAsync(x => x.Id == id);

        if (tour == null)
        {
            return NotFound();
        }

        var orderedStops = tour.Stops.OrderBy(x => x.SortOrder).ToList();
        var stopPoiIds = string.Join(",", orderedStops.Select(x => x.PoiId));
        var stopConfigJson = SerializeStopConfigs(orderedStops.Select(x => new TourStopDraft
        {
            PoiId = x.PoiId,
            AutoPlay = x.AutoPlay,
            Note = x.Note
        }));
        return View(BuildForm(tour, stopPoiIds, stopConfigJson));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(TourFormViewModel model)
    {
        var stopConfigs = ParseStopConfigs(model.StopConfigJson, model.StopPoiIds);
        var translationInputs = NormalizeTourTranslationInputs(model.Translations, model.Tour.Language, model.Tour.Name, model.Tour.Description);
        model.StopPoiIds = string.Join(",", stopConfigs.Select(x => x.PoiId));
        model.StopConfigJson = SerializeStopConfigs(stopConfigs);
        ModelState.Remove(nameof(model.StopPoiIds));
        ModelState.Remove(nameof(model.StopConfigJson));
        if (string.IsNullOrWhiteSpace(model.StopPoiIds))
        {
            ModelState.AddModelError(nameof(model.StopPoiIds), "Hãy chọn ít nhất một POI cho tour.");
            ModelState.AddModelError(string.Empty, "Tour chưa có điểm dừng nào. Hãy thêm ít nhất một POI vào danh sách điểm dừng trước khi lưu.");
        }

        if (!ModelState.IsValid)
        {
            return View(BuildForm(model.Tour, model.StopPoiIds, model.StopConfigJson, translationInputs));
        }

        var existing = await _context.Tours
            .Include(x => x.Translations)
            .Include(x => x.Stops)
            .FirstOrDefaultAsync(x => x.Id == model.Tour.Id);

        if (existing == null)
        {
            return NotFound();
        }

        existing.Name = model.Tour.Name;
        existing.Description = model.Tour.Description;
        existing.Language = model.Tour.Language;
        existing.CoverImageUrl = await SaveImageAsync(model.CoverImageFile, model.Tour.CoverImageUrl);
        existing.IsActive = model.Tour.IsActive;
        existing.EstimatedDurationMinutes = model.Tour.EstimatedDurationMinutes;
        existing.UpdatedAt = DateTime.UtcNow;

        _context.TourStops.RemoveRange(existing.Stops);
        existing.Stops = BuildStops(stopConfigs, existing.Id);
        _context.TourTranslations.RemoveRange(existing.Translations);
        existing.Translations = BuildTranslations(translationInputs, existing.Language, existing.Id);

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var tour = await _context.Tours.FindAsync(id);
        if (tour != null)
        {
            _context.Tours.Remove(tour);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    private TourFormViewModel BuildForm(Tour tour, string stopPoiIds = "", string? stopConfigJson = null, List<TourTranslationInputViewModel>? translations = null)
    {
        var normalizedStopConfigs = ParseStopConfigs(stopConfigJson, stopPoiIds);
        var translationInputs = translations ?? BuildTranslationInputs(tour);
        return new TourFormViewModel
        {
            Tour = tour,
            StopPoiIds = string.Join(",", normalizedStopConfigs.Select(x => x.PoiId)),
            StopConfigJson = SerializeStopConfigs(normalizedStopConfigs),
            PoiOptions = AdminPoiScopeHelper.GetScopedPoiQuery(_context)
                .OrderBy(x => x.Name)
                .Select(x => new SelectListItem($"{x.Name} (#{x.Id})", x.Id.ToString()))
                .ToList(),
            LanguageOptions = _context.LanguageOptions
                .Where(x => x.IsActive)
                .OrderBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Select(x => new SelectListItem($"{x.Name} ({x.Code})", x.Code, x.Code == tour.Language))
                .ToList(),
            Translations = translationInputs
        };
    }

    private List<TourTranslationInputViewModel> BuildTranslationInputs(Tour tour)
    {
        var languages = _context.LanguageOptions
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToList();

        return languages.Select(language =>
        {
            var existing = tour.Translations.FirstOrDefault(x => string.Equals(x.Language, language.Code, StringComparison.OrdinalIgnoreCase));
            var isDefault = string.Equals(language.Code, tour.Language, StringComparison.OrdinalIgnoreCase);
            return new TourTranslationInputViewModel
            {
                Language = language.Code,
                LanguageLabel = string.IsNullOrWhiteSpace(language.NativeName)
                    ? $"{language.Name} ({language.Code})"
                    : $"{language.Name} - {language.NativeName} ({language.Code})",
                Title = isDefault ? tour.Name : existing?.Title ?? string.Empty,
                Description = isDefault ? tour.Description : existing?.Description ?? string.Empty
            };
        }).ToList();
    }

    private static List<TourTranslationInputViewModel> NormalizeTourTranslationInputs(
        IEnumerable<TourTranslationInputViewModel>? inputs,
        string defaultLanguage,
        string defaultTitle,
        string defaultDescription)
    {
        var normalized = (inputs ?? Enumerable.Empty<TourTranslationInputViewModel>())
            .Where(x => !string.IsNullOrWhiteSpace(x.Language))
            .GroupBy(x => x.Language.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                var item = group.First();
                return new TourTranslationInputViewModel
                {
                    Language = item.Language.Trim(),
                    LanguageLabel = item.LanguageLabel?.Trim() ?? string.Empty,
                    Title = item.Title?.Trim() ?? string.Empty,
                    Description = item.Description?.Trim() ?? string.Empty
                };
            })
            .ToList();

        var defaultEntry = normalized.FirstOrDefault(x => string.Equals(x.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase));
        if (defaultEntry == null)
        {
            normalized.Add(new TourTranslationInputViewModel
            {
                Language = defaultLanguage,
                Title = defaultTitle?.Trim() ?? string.Empty,
                Description = defaultDescription?.Trim() ?? string.Empty
            });
        }
        else
        {
            defaultEntry.Title = defaultTitle?.Trim() ?? string.Empty;
            defaultEntry.Description = defaultDescription?.Trim() ?? string.Empty;
        }

        return normalized;
    }

    private static List<TourTranslation> BuildTranslations(
        IEnumerable<TourTranslationInputViewModel> inputs,
        string defaultLanguage,
        int tourId)
    {
        return inputs
            .Where(x => !string.Equals(x.Language, defaultLanguage, StringComparison.OrdinalIgnoreCase))
            .Where(x => !string.IsNullOrWhiteSpace(x.Title) || !string.IsNullOrWhiteSpace(x.Description))
            .Select(x => new TourTranslation
            {
                TourId = tourId,
                Language = x.Language,
                Title = x.Title?.Trim() ?? string.Empty,
                Description = x.Description?.Trim() ?? string.Empty
            })
            .ToList();
    }

    private static List<TourStop> BuildStops(IEnumerable<TourStopDraft> stopConfigs, int tourId)
    {
        return stopConfigs
            .Select((stop, index) => new TourStop
            {
                TourId = tourId,
                PoiId = stop.PoiId,
                SortOrder = index + 1,
                AutoPlay = stop.AutoPlay,
                Note = stop.Note
            })
            .ToList();
    }

    private static List<TourStopDraft> ParseStopConfigs(string? rawJson, string? fallbackPoiIds)
    {
        List<TourStopDraft>? parsedStops = null;
        if (!string.IsNullOrWhiteSpace(rawJson))
        {
            try
            {
                parsedStops = JsonSerializer.Deserialize<List<TourStopDraft>>(rawJson);
            }
            catch (JsonException)
            {
                parsedStops = null;
            }
        }

        var normalized = (parsedStops ?? new List<TourStopDraft>())
            .Where(x => x.PoiId > 0)
            .GroupBy(x => x.PoiId)
            .Select(group =>
            {
                var item = group.First();
                return new TourStopDraft
                {
                    PoiId = item.PoiId,
                    AutoPlay = item.AutoPlay,
                    Note = (item.Note ?? string.Empty).Trim()
                };
            })
            .ToList();

        if (normalized.Count > 0)
        {
            return normalized;
        }

        return (fallbackPoiIds ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => int.TryParse(value, out _))
            .Distinct()
            .Select(value => new TourStopDraft
            {
                PoiId = int.Parse(value),
                AutoPlay = true,
                Note = string.Empty
            })
            .ToList();
    }

    private static string SerializeStopConfigs(IEnumerable<TourStopDraft> stopConfigs)
    {
        return JsonSerializer.Serialize(stopConfigs.Select(x => new TourStopDraft
        {
            PoiId = x.PoiId,
            AutoPlay = x.AutoPlay,
            Note = x.Note
        }));
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

    private sealed class TourStopDraft
    {
        public int PoiId { get; set; }
        public bool AutoPlay { get; set; } = true;
        public string Note { get; set; } = string.Empty;
    }
}
