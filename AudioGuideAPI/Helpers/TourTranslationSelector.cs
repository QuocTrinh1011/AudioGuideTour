using AudioGuideAPI.Models;

namespace AudioGuideAPI.Helpers;

public static class TourTranslationSelector
{
    public static TourTranslation? Select(IEnumerable<TourTranslation>? translations, string? language)
    {
        if (translations == null)
        {
            return null;
        }

        var source = translations.ToList();
        if (source.Count == 0)
        {
            return null;
        }

        var normalizedLanguage = (language ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(normalizedLanguage))
        {
            return source.FirstOrDefault();
        }

        var languageRoot = normalizedLanguage
            .Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();

        return source.FirstOrDefault(x => string.Equals(x.Language, normalizedLanguage, StringComparison.OrdinalIgnoreCase))
            ?? source.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(languageRoot) &&
                x.Language.StartsWith(languageRoot, StringComparison.OrdinalIgnoreCase));
    }
}
