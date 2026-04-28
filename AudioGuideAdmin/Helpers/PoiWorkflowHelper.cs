using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Helpers;

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
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static void ApplySubmissionToPoi(Poi poi, PoiSubmission submission)
    {
        poi.OwnerId = submission.OwnerId;
        poi.Name = submission.Name;
        poi.Category = submission.Category;
        poi.Summary = submission.Summary;
        poi.Description = submission.Description;
        poi.Address = submission.Address;
        poi.Latitude = submission.Latitude;
        poi.Longitude = submission.Longitude;
        poi.Radius = submission.Radius;
        poi.ApproachRadiusMeters = submission.ApproachRadiusMeters;
        poi.Priority = submission.Priority;
        poi.DebounceSeconds = submission.DebounceSeconds;
        poi.CooldownSeconds = submission.CooldownSeconds;
        poi.TriggerMode = submission.TriggerMode;
        poi.ImageUrl = submission.ImageUrl;
        poi.MapUrl = submission.MapUrl;
        poi.IsActive = submission.IsActive;
        poi.AudioMode = NormalizeAudioMode(submission.AudioMode, submission.AudioUrl);
        poi.AudioUrl = submission.AudioUrl;
        poi.TtsScript = submission.TtsScript;
        poi.DefaultLanguage = submission.DefaultLanguage;
        poi.EstimatedDurationSeconds = submission.EstimatedDurationSeconds;
        poi.UpdatedAt = DateTime.UtcNow;
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

    public static void ApplyTranslationSubmissionsToPoi(
        Poi poi,
        PoiSubmission submission,
        IEnumerable<PoiTranslationSubmission> translationSubmissions)
    {
        poi.Translations ??= new List<PoiTranslation>();

        var liveMap = poi.Translations
            .GroupBy(x => x.Language, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var translationSubmission in translationSubmissions)
        {
            var language = translationSubmission.Language.Trim();
            if (string.IsNullOrWhiteSpace(language))
            {
                continue;
            }

            if (string.Equals(language, submission.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
            {
                if (liveMap.TryGetValue(language, out var defaultLanguageLiveTranslation))
                {
                    poi.Translations.Remove(defaultLanguageLiveTranslation);
                    liveMap.Remove(language);
                }

                continue;
            }

            if (!HasMeaningfulTranslationContent(translationSubmission))
            {
                if (liveMap.TryGetValue(language, out var emptyLiveTranslation))
                {
                    poi.Translations.Remove(emptyLiveTranslation);
                    liveMap.Remove(language);
                }

                continue;
            }

            if (!liveMap.TryGetValue(language, out var liveTranslation))
            {
                liveTranslation = new PoiTranslation
                {
                    PoiId = poi.Id,
                    Language = language
                };
                poi.Translations.Add(liveTranslation);
                liveMap[language] = liveTranslation;
            }

            liveTranslation.Language = language;
            liveTranslation.Title = translationSubmission.Title?.Trim() ?? string.Empty;
            liveTranslation.Summary = translationSubmission.Summary?.Trim() ?? string.Empty;
            liveTranslation.Description = translationSubmission.Description?.Trim() ?? string.Empty;
            liveTranslation.AudioUrl = translationSubmission.AudioUrl?.Trim() ?? string.Empty;
            liveTranslation.TtsScript = translationSubmission.TtsScript?.Trim() ?? string.Empty;
            liveTranslation.VoiceName = translationSubmission.VoiceName?.Trim() ?? string.Empty;
            liveTranslation.IsPublished = true;
            liveTranslation.UpdatedAt = DateTime.UtcNow;
        }
    }
}
