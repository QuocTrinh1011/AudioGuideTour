using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Helpers;

public static class PoiImageSuggestionHelper
{
    public static string? Suggest(Poi poi)
    {
        var haystack = $"{poi.Name} {poi.Address} {poi.Category}".ToLowerInvariant();

        if (ContainsAny(haystack, "xom chieu", "xuan chieu"))
        {
            return "/images/poi-xuan-chieu-bus-stop.png";
        }

        if (ContainsAny(haystack, "khanh hoi"))
        {
            return "/images/poi-khanh-hoi-bus-stop.png";
        }

        if (ContainsAny(haystack, "vinh hoi", "hoang dieu"))
        {
            return "/images/poi-vinh-hoi-bus-stop.png";
        }

        if (ContainsAny(haystack, "nhip song", "culture"))
        {
            return "/images/poi-vinh-khanh-street-life.png";
        }

        if (ContainsAny(haystack, "quan oc", "cum quan oc"))
        {
            return "/images/poi-vinh-khanh-seafood-cluster.png";
        }

        if (ContainsAny(haystack, "vinh khanh", "pho am thuc", "food-street"))
        {
            return "/images/poi-vinh-khanh-food-street.png";
        }

        return null;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(value.Contains);
    }
}
