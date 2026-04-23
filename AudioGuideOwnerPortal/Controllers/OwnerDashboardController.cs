using AudioGuideOwnerPortal.Data;
using AudioGuideOwnerPortal.Helpers;
using AudioGuideOwnerPortal.Models;
using AudioGuideOwnerPortal.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideOwnerPortal.Controllers;

public class OwnerDashboardController : Controller
{
    private readonly AppDbContext _context;

    public OwnerDashboardController(AppDbContext context)
    {
        _context = context;
    }

    public async Task<IActionResult> Index()
    {
        var owner = await GetCurrentOwnerAsync();
        if (owner == null)
        {
            return RedirectToAction("Login", "OwnerAuth");
        }

        var activeLanguages = await _context.LanguageOptions
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .ToListAsync();

        var livePois = await _context.Pois
            .AsNoTracking()
            .Include(x => x.Translations)
            .Where(x => x.OwnerId == owner.Id)
            .OrderBy(x => x.Name)
            .ToListAsync();

        var submissions = await _context.PoiSubmissions
            .AsNoTracking()
            .Where(x => x.OwnerId == owner.Id)
            .ToListAsync();

        var livePoiIds = livePois.Select(x => x.Id).ToList();
        var visits = livePoiIds.Count == 0
            ? new List<VisitHistory>()
            : await _context.VisitHistories
                .AsNoTracking()
                .Where(x => livePoiIds.Contains(x.PoiId))
                .ToListAsync();

        var validVisits = visits
            .Where(x => x.Duration > 0 && x.Duration <= 1800)
            .ToList();

        var poiLookup = livePois.ToDictionary(x => x.Id);

        var topPois = visits
            .GroupBy(x => x.PoiId)
            .Select(group =>
            {
                var poi = poiLookup[group.Key];
                var poiValidVisits = validVisits.Where(x => x.PoiId == group.Key).ToList();
                return new OwnerTopPoiStatViewModel
                {
                    PoiId = group.Key,
                    PoiName = poi.Name,
                    Category = poi.Category,
                    ListenCount = group.Count(),
                    AverageListenSeconds = poiValidVisits.Count == 0 ? 0 : poiValidVisits.Average(x => x.Duration)
                };
            })
            .OrderByDescending(x => x.ListenCount)
            .ThenBy(x => x.PoiName)
            .Take(5)
            .ToList();

        var languageUsage = activeLanguages
            .Select(language =>
            {
                var count = visits.Count(x => string.Equals(x.Language, language.Code, StringComparison.OrdinalIgnoreCase));
                return new OwnerLanguageUsageViewModel
                {
                    Language = language.Code,
                    Label = string.IsNullOrWhiteSpace(language.NativeName)
                        ? $"{language.Name} ({language.Code})"
                        : $"{language.Name} - {language.NativeName} ({language.Code})",
                    ListenCount = count,
                    Percentage = visits.Count == 0 ? 0 : Math.Round((double)count * 100 / visits.Count, 1)
                };
            })
            .OrderByDescending(x => x.ListenCount)
            .ThenBy(x => x.Label)
            .ToList();

        var contentHealth = BuildContentHealth(livePois, activeLanguages);

        var topLanguage = languageUsage.FirstOrDefault(x => x.ListenCount > 0);
        var topPoi = topPois.FirstOrDefault();

        var model = new OwnerDashboardViewModel
        {
            Owner = owner,
            LivePoiCount = livePois.Count,
            ActivePoiCount = livePois.Count(x => x.IsActive),
            DraftSubmissionCount = submissions.Count(x => string.Equals(x.Status, PoiSubmissionStatus.Draft, StringComparison.OrdinalIgnoreCase)),
            PendingSubmissionCount = submissions.Count(x => string.Equals(x.Status, PoiSubmissionStatus.PendingReview, StringComparison.OrdinalIgnoreCase)),
            ChangesRequestedCount = submissions.Count(x => string.Equals(x.Status, PoiSubmissionStatus.ChangesRequested, StringComparison.OrdinalIgnoreCase)),
            ApprovedSubmissionCount = submissions.Count(x => string.Equals(x.Status, PoiSubmissionStatus.Approved, StringComparison.OrdinalIgnoreCase)),
            TotalListenCount = visits.Count,
            UniqueVisitorCount = visits
                .Where(x => !string.IsNullOrWhiteSpace(x.UserId))
                .Select(x => x.UserId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count(),
            AverageListenSeconds = validVisits.Count == 0 ? 0 : validVisits.Average(x => x.Duration),
            TopLanguageLabel = topLanguage?.Label ?? "Chưa có dữ liệu",
            TopLanguageListenCount = topLanguage?.ListenCount ?? 0,
            TopPoiName = topPoi?.PoiName ?? "Chưa có dữ liệu",
            TopPoiListenCount = topPoi?.ListenCount ?? 0,
            TopPois = topPois,
            LanguageUsage = languageUsage,
            ContentHealth = contentHealth
        };

        return View(model);
    }

    private async Task<ShopOwner?> GetCurrentOwnerAsync()
    {
        var ownerId = OwnerSessionHelper.GetOwnerId(HttpContext);
        if (string.IsNullOrWhiteSpace(ownerId))
        {
            return null;
        }

        return await _context.ShopOwners.AsNoTracking().FirstOrDefaultAsync(x => x.Id == ownerId);
    }

    private static OwnerContentHealthViewModel BuildContentHealth(
        IEnumerable<Poi> livePois,
        IReadOnlyCollection<LanguageOption> activeLanguages)
    {
        var nonDefaultLanguages = activeLanguages
            .Select(x => x.Code)
            .ToList();

        var result = new OwnerContentHealthViewModel();

        foreach (var poi in livePois)
        {
            var translationLookup = poi.Translations
                .Where(x => x.IsPublished)
                .GroupBy(x => x.Language, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

            var expectedNonDefaultLanguages = nonDefaultLanguages
                .Where(code => !string.Equals(code, poi.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var hasMissingTranslation = expectedNonDefaultLanguages.Any(languageCode =>
                !translationLookup.TryGetValue(languageCode, out var translation) ||
                string.IsNullOrWhiteSpace(translation.Title) ||
                string.IsNullOrWhiteSpace(translation.Description));

            if (hasMissingTranslation)
            {
                result.MissingTranslationPoiCount++;
            }

            var requiresFallbackAudio = !string.Equals(poi.AudioMode, "tts", StringComparison.OrdinalIgnoreCase);
            if (requiresFallbackAudio && string.IsNullOrWhiteSpace(poi.AudioUrl))
            {
                result.MissingAudioFallbackPoiCount++;
            }

            if (string.IsNullOrWhiteSpace(poi.ImageUrl))
            {
                result.MissingImagePoiCount++;
            }

            if (string.IsNullOrWhiteSpace(poi.TtsScript))
            {
                result.MissingDefaultTtsPoiCount++;
            }

            var hasAnyNonDefaultTranslation = expectedNonDefaultLanguages.Any(languageCode =>
                translationLookup.TryGetValue(languageCode, out var translation) &&
                (!string.IsNullOrWhiteSpace(translation.Title) ||
                 !string.IsNullOrWhiteSpace(translation.Description) ||
                 !string.IsNullOrWhiteSpace(translation.TtsScript)));

            if (!hasAnyNonDefaultTranslation)
            {
                result.SingleLanguagePoiCount++;
            }
        }

        return result;
    }
}
