using AudioGuideAPI.Data;
using AudioGuideAPI.Models;

namespace AudioGuideAPI.Helpers;

public static class ApiPoiScopeHelper
{
    public static IQueryable<Poi> GetScopedPoiQuery(AppDbContext context)
    {
        var ownerManagedPois = context.Pois.Where(x => x.OwnerId != null && x.OwnerId != string.Empty);
        return ownerManagedPois.Any()
            ? ownerManagedPois
            : context.Pois;
    }

    public static List<int> GetScopedPoiIds(AppDbContext context)
    {
        return GetScopedPoiQuery(context)
            .Select(x => x.Id)
            .ToList();
    }
}
