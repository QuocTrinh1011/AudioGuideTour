using Microsoft.AspNetCore.Http;

namespace AudioGuideAdmin.Helpers;

public static class OwnerSessionHelper
{
    public const string OwnerIdKey = "ownerId";
    public const string OwnerNameKey = "ownerName";

    public static void SignIn(HttpContext httpContext, string ownerId, string ownerName)
    {
        httpContext.Session.SetString(OwnerIdKey, ownerId);
        httpContext.Session.SetString(OwnerNameKey, ownerName);
    }

    public static void SignOut(HttpContext httpContext)
    {
        httpContext.Session.Remove(OwnerIdKey);
        httpContext.Session.Remove(OwnerNameKey);
    }

    public static string? GetOwnerId(HttpContext httpContext)
        => httpContext.Session.GetString(OwnerIdKey);

    public static string GetOwnerDisplayName(HttpContext httpContext)
        => httpContext.Session.GetString(OwnerNameKey) ?? "Chủ quán";
}
