using AudioGuideAdmin.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideAdmin.Controllers.Data;

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
    public DbSet<RegistrationPlan> RegistrationPlans => Set<RegistrationPlan>();
    public DbSet<MembershipRegistration> MembershipRegistrations => Set<MembershipRegistration>();
    public DbSet<CustomerAccount> CustomerAccounts => Set<CustomerAccount>();
    public DbSet<ShopOwner> ShopOwners => Set<ShopOwner>();
    public DbSet<PoiSubmission> PoiSubmissions => Set<PoiSubmission>();
    public DbSet<PoiTranslationSubmission> PoiTranslationSubmissions => Set<PoiTranslationSubmission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>()
            .ToTable("AdminUsers");

        modelBuilder.Entity<User>()
            .HasIndex(x => x.Username)
            .IsUnique();

        modelBuilder.Entity<VisitorProfile>()
            .ToTable("Visitors");

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

        modelBuilder.Entity<Poi>()
            .HasOne(x => x.Owner)
            .WithMany(x => x.Pois)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.SetNull);

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

        modelBuilder.Entity<CustomerAccount>()
            .HasIndex(x => x.Phone)
            .IsUnique();

        modelBuilder.Entity<CustomerAccount>()
            .HasIndex(x => x.Email)
            .IsUnique();

        modelBuilder.Entity<CustomerAccount>()
            .HasIndex(x => x.SessionToken);

        modelBuilder.Entity<CustomerAccount>()
            .HasOne(x => x.Registration)
            .WithOne()
            .HasForeignKey<CustomerAccount>(x => x.RegistrationId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Phone);

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Email);

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<PoiSubmission>()
            .HasIndex(x => new { x.OwnerId, x.Status });

        modelBuilder.Entity<PoiSubmission>()
            .HasOne(x => x.Owner)
            .WithMany(x => x.Submissions)
            .HasForeignKey(x => x.OwnerId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<PoiSubmission>()
            .HasOne(x => x.Poi)
            .WithMany()
            .HasForeignKey(x => x.PoiId)
            .OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<PoiTranslationSubmission>()
            .HasIndex(x => new { x.SubmissionId, x.Language })
            .IsUnique();

        modelBuilder.Entity<PoiTranslationSubmission>()
            .HasOne(x => x.Submission)
            .WithMany(x => x.TranslationSubmissions)
            .HasForeignKey(x => x.SubmissionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
