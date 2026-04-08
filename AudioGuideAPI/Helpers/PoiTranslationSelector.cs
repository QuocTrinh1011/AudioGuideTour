using AudioGuideAPI.Models;

namespace AudioGuideAPI.Helpers;

public static class PoiTranslationSelector
{
    public static PoiTranslation? Select(IEnumerable<PoiTranslation>? translations, string? language, bool publishedOnly = true)
    {
        if (translations == null)
        {
            return null;
        }

        var source = publishedOnly
            ? translations.Where(x => x.IsPublished).ToList()
            : translations.ToList();

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
