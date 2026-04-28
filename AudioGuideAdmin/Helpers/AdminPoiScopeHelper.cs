using AudioGuideAdmin.Controllers.Data;
using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Helpers;

public static class AdminPoiScopeHelper
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
