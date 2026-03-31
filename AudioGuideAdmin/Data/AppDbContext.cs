using AudioGuideAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<LanguageOption> LanguageOptions => Set<LanguageOption>();
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<QRCode> QRCodes => Set<QRCode>();
    public DbSet<VisitorProfile> Visitors => Set<VisitorProfile>();
    public DbSet<UserTracking> UserTrackings => Set<UserTracking>();
    public DbSet<VisitHistory> VisitHistories => Set<VisitHistory>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourStop> TourStops => Set<TourStop>();
    public DbSet<GeofenceTrigger> GeofenceTriggers => Set<GeofenceTrigger>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .ToTable("AdminUsers");

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<VisitorProfile>()
            .ToTable("Users");

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<LanguageOption>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<PoiTranslation>()
            .HasIndex(x => new { x.PoiId, x.Language })
            .IsUnique();

        modelBuilder.Entity<PoiTranslation>()
            .HasOne(x => x.Poi)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<QRCode>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<QRCode>()
            .HasOne(x => x.Poi)
            .WithMany()
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TourStop>()
            .HasIndex(x => new { x.TourId, x.SortOrder })
            .IsUnique();

        modelBuilder.Entity<TourStop>()
            .HasOne(x => x.Tour)
            .WithMany(x => x.Stops)
            .HasForeignKey(x => x.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TourStop>()
            .HasOne(x => x.Poi)
            .WithMany(x => x.TourStops)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
