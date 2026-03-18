using Microsoft.EntityFrameworkCore;
using AudioGuideAPI.Models;

namespace AudioGuideAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }
    public DbSet<QRCode> QRCodes { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<Poi> Pois { get; set; }

    public DbSet<PoiTranslation> PoiTranslations { get; set; }

    public DbSet<VisitHistory> VisitHistories { get; set; }

    public DbSet<UserTracking> UserTrackings { get; set; }
}