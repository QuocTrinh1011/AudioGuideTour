using AudioGuideAPI.Models;

namespace AudioGuideAPI.Helpers;

public static class PoiAudioSelector
{
    private static readonly HashSet<string> BrokenVietnameseDemoAudioPaths = new(StringComparer.OrdinalIgnoreCase)
    {
        "/audio/poi-vinh-khanh-food-street-vi.wav",
        "/audio/poi-vinh-khanh-seafood-cluster-vi.wav",
        "/audio/poi-khanh-hoi-bus-stop-vi.wav",
        "/audio/poi-vinh-hoi-bus-stop-vi.wav",
        "/audio/poi-xuan-chieu-bus-stop-vi.wav",
        "/audio/poi-vinh-khanh-street-life-vi.wav"
    };

    public static string Resolve(Poi poi, PoiTranslation? translation)
    {
        var translationLanguage = translation?.Language?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(translation?.AudioUrl))
        {
            return Sanitize(translation.AudioUrl, translationLanguage);
        }

        if (translation != null &&
            !string.IsNullOrWhiteSpace(translationLanguage) &&
            !string.Equals(translationLanguage, poi.DefaultLanguage, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return Sanitize(poi.AudioUrl, string.IsNullOrWhiteSpace(translationLanguage) ? poi.DefaultLanguage : translationLanguage);
    }

    private static string Sanitize(string? audioUrl, string? language)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
        {
            return string.Empty;
        }

        var normalized = audioUrl.Trim();
        if (Uri.TryCreate(normalized, UriKind.Absolute, out var absolute))
        {
            normalized = absolute.AbsolutePath;
        }

        if (!string.IsNullOrWhiteSpace(language) &&
            language.StartsWith("vi", StringComparison.OrdinalIgnoreCase) &&
            BrokenVietnameseDemoAudioPaths.Contains(normalized))
        {
            return string.Empty;
        }

        return audioUrl.Trim();
    }
}
