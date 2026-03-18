using Microsoft.EntityFrameworkCore;
using AudioGuideAdmin.Models;

namespace AudioGuideAdmin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }
    public DbSet<User> Users { get; set; }
    public DbSet<Poi> Pois { get; set; }

    public DbSet<PoiTranslation> PoiTranslations { get; set; }

    public DbSet<UserTracking> UserTrackings { get; set; }

    public DbSet<VisitHistory> VisitHistories { get; set; }
}