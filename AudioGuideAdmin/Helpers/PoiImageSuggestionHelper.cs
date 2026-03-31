using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Helpers;

public static class PoiImageSuggestionHelper
{
    public static string? Suggest(Poi poi)
    {
        var haystack = $"{poi.Name} {poi.Address} {poi.Category}".ToLowerInvariant();

        if (ContainsAny(haystack, "xom chieu", "xóm chiếu", "xuan chieu", "xuân chiếu"))
        {
            return "https://commons.wikimedia.org/wiki/Special:FilePath/Nh%C3%A0_th%E1%BB%9D_X%C3%B3m_Chi%E1%BA%BFu.jpg";
        }

        if (ContainsAny(haystack, "khanh hoi", "khánh hội"))
        {
            return "https://commons.wikimedia.org/wiki/Special:FilePath/B%E1%BA%BFn_Nh%C3%A0_R%E1%BB%93ng_v%C3%A0_c%E1%BA%A7u_Kh%C3%A1nh_H%E1%BB%99i.JPG";
        }

        if (ContainsAny(haystack, "vinh hoi", "vĩnh hội", "hoang dieu", "hoàng diệu"))
        {
            return "https://commons.wikimedia.org/wiki/Special:FilePath/M%E1%BB%99t_g%C3%B3c_Qu%E1%BA%ADn_4%2C_TPHCM_ch%E1%BB%95_%C4%91%C6%B0%E1%BB%9Dng_Ho%C3%A0ng_Di%E1%BB%87u_%281%29.jpg";
        }

        if (ContainsAny(haystack, "vinh khanh", "vĩnh khánh", "pho am thuc", "ẩm thực", "food-street"))
        {
            return "https://commons.wikimedia.org/wiki/Special:FilePath/Saigon_district_4%2C_seen_from_bus_20.jpg";
        }

        return null;
    }

    private static bool ContainsAny(string value, params string[] needles)
    {
        return needles.Any(value.Contains);
    }
}
