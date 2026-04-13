using AudioGuideAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAPI.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<QRCode> QRCodes => Set<QRCode>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<LanguageOption> LanguageOptions => Set<LanguageOption>();
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<UserTracking> UserTrackings => Set<UserTracking>();
    public DbSet<VisitHistory> VisitHistories => Set<VisitHistory>();
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<TourStop> TourStops => Set<TourStop>();
    public DbSet<GeofenceTrigger> GeofenceTriggers => Set<GeofenceTrigger>();
    public DbSet<RegistrationPlan> RegistrationPlans => Set<RegistrationPlan>();
    public DbSet<MembershipRegistration> MembershipRegistrations => Set<MembershipRegistration>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .ToTable("Visitors");

        modelBuilder.Entity<Poi>()
            .Property(x => x.Name)
            .HasMaxLength(200);

        modelBuilder.Entity<Poi>()
            .Property(x => x.Category)
            .HasMaxLength(100);

        modelBuilder.Entity<Category>()
            .Property(x => x.Slug)
            .HasMaxLength(100);

        modelBuilder.Entity<Category>()
            .Property(x => x.Name)
            .HasMaxLength(150);

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<LanguageOption>()
            .Property(x => x.Code)
            .HasMaxLength(20);

        modelBuilder.Entity<LanguageOption>()
            .Property(x => x.Name)
            .HasMaxLength(100);

        modelBuilder.Entity<LanguageOption>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<Poi>()
            .Property(x => x.TriggerMode)
            .HasMaxLength(40);

        modelBuilder.Entity<Poi>()
            .Property(x => x.AudioMode)
            .HasMaxLength(40);

        modelBuilder.Entity<PoiTranslation>()
            .HasIndex(x => new { x.PoiId, x.Language })
            .IsUnique();

        modelBuilder.Entity<PoiTranslation>()
            .HasOne(x => x.Poi)
            .WithMany(x => x.Translations)
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Tour>()
            .Property(x => x.Name)
            .HasMaxLength(200);

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

        modelBuilder.Entity<QRCode>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<QRCode>()
            .HasOne(x => x.Poi)
            .WithMany()
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<RegistrationPlan>()
            .Property(x => x.Code)
            .HasMaxLength(50);

        modelBuilder.Entity<RegistrationPlan>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<MembershipRegistration>()
            .HasIndex(x => x.VisitorId);

        modelBuilder.Entity<MembershipRegistration>()
            .HasIndex(x => x.DeviceId);

        modelBuilder.Entity<MembershipRegistration>()
            .HasIndex(x => x.OrderCode)
            .IsUnique();

        modelBuilder.Entity<MembershipRegistration>()
            .HasOne(x => x.RegistrationPlan)
            .WithMany(x => x.Registrations)
            .HasForeignKey(x => x.RegistrationPlanId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
