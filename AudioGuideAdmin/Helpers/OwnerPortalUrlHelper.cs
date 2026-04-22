namespace AudioGuideAdmin.Helpers;

public static class OwnerPortalUrlHelper
{
    public static string Build(IConfiguration configuration, string relativePath, IDictionary<string, string?>? query = null)
    {
        var baseUrl = configuration["OwnerPortal:BaseUrl"]?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            baseUrl = "https://localhost:7051";
        }

        baseUrl = baseUrl.TrimEnd('/');
        relativePath = "/" + relativePath.TrimStart('/');

        if (query == null || query.Count == 0)
        {
            return baseUrl + relativePath;
        }

        var queryString = string.Join("&",
            query
                .Where(x => !string.IsNullOrWhiteSpace(x.Value))
                .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));

        return string.IsNullOrWhiteSpace(queryString)
            ? baseUrl + relativePath
            : $"{baseUrl}{relativePath}?{queryString}";
    }
}
