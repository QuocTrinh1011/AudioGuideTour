using AudioGuideOwnerPortal.Models;

namespace AudioGuideOwnerPortal.Helpers;

public static class PoiWorkflowHelper
{
    public static string NormalizeAudioMode(string? requestedMode, string? audioUrl)
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

    public static PoiSubmission CreateSubmissionFromPoi(Poi poi, string ownerId)
    {
        return new PoiSubmission
        {
            PoiId = poi.Id,
            OwnerId = ownerId,
            SubmissionType = "update",
            Status = PoiSubmissionStatus.Draft,
            Name = poi.Name,
            Category = poi.Category,
            Summary = poi.Summary,
            Description = poi.Description,
            Address = poi.Address,
            Latitude = poi.Latitude,
            Longitude = poi.Longitude,
            Radius = poi.Radius,
            ApproachRadiusMeters = poi.ApproachRadiusMeters,
            Priority = poi.Priority,
            DebounceSeconds = poi.DebounceSeconds,
            CooldownSeconds = poi.CooldownSeconds,
            TriggerMode = poi.TriggerMode,
            ImageUrl = poi.ImageUrl,
            MapUrl = poi.MapUrl,
            IsActive = poi.IsActive,
            AudioMode = poi.AudioMode,
            AudioUrl = poi.AudioUrl,
            TtsScript = poi.TtsScript,
            DefaultLanguage = poi.DefaultLanguage,
            EstimatedDurationSeconds = poi.EstimatedDurationSeconds,
            TranslationSubmissions = poi.Translations
                .OrderBy(x => x.Language, StringComparer.OrdinalIgnoreCase)
                .Select((translation, index) => new PoiTranslationSubmission
                {
                    Language = translation.Language,
                    Title = translation.Title,
                    Summary = translation.Summary,
                    Description = translation.Description,
                    AudioUrl = translation.AudioUrl,
                    TtsScript = translation.TtsScript,
                    VoiceName = translation.VoiceName,
                    SortOrder = index,
                    UpdatedAt = DateTime.UtcNow
                })
                .ToList(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static List<PoiTranslationSubmission> EnsureSubmissionTranslations(
        IEnumerable<LanguageOption> languages,
        string defaultLanguage,
        IEnumerable<PoiTranslationSubmission>? existingSubmissions = null)
    {
        var existingMap = (existingSubmissions ?? Enumerable.Empty<PoiTranslationSubmission>())
            .GroupBy(x => x.Language, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        return languages
            .OrderBy(x => x.SortOrder)
            .ThenBy(x => x.Name)
            .Select((language, index) =>
            {
                if (existingMap.TryGetValue(language.Code, out var existing))
                {
                    existing.SortOrder = index;
                    return existing;
                }

                return new PoiTranslationSubmission
                {
                    Language = language.Code,
                    SortOrder = index,
                    Title = string.Equals(language.Code, defaultLanguage, StringComparison.OrdinalIgnoreCase) ? string.Empty : string.Empty,
                    Summary = string.Empty,
                    Description = string.Empty,
                    AudioUrl = string.Empty,
                    TtsScript = string.Empty,
                    VoiceName = string.Empty,
                    UpdatedAt = DateTime.UtcNow
                };
            })
            .ToList();
    }

    public static bool HasMeaningfulTranslationContent(PoiTranslationSubmission submission)
    {
        return !string.IsNullOrWhiteSpace(submission.Title)
               || !string.IsNullOrWhiteSpace(submission.Summary)
               || !string.IsNullOrWhiteSpace(submission.Description)
               || !string.IsNullOrWhiteSpace(submission.AudioUrl)
               || !string.IsNullOrWhiteSpace(submission.TtsScript)
               || !string.IsNullOrWhiteSpace(submission.VoiceName);
    }
}
