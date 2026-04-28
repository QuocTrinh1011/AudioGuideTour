using AudioGuideOwnerPortal.Models;
using Microsoft.EntityFrameworkCore;

namespace AudioGuideOwnerPortal.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<ShopOwner> ShopOwners => Set<ShopOwner>();
    public DbSet<PoiSubmission> PoiSubmissions => Set<PoiSubmission>();
    public DbSet<Poi> Pois => Set<Poi>();
    public DbSet<PoiTranslation> PoiTranslations => Set<PoiTranslation>();
    public DbSet<VisitHistory> VisitHistories => Set<VisitHistory>();
    public DbSet<PoiTranslationSubmission> PoiTranslationSubmissions => Set<PoiTranslationSubmission>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<LanguageOption> LanguageOptions => Set<LanguageOption>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Category>()
            .HasIndex(x => x.Slug)
            .IsUnique();

        modelBuilder.Entity<LanguageOption>()
            .HasIndex(x => x.Code)
            .IsUnique();

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Phone);

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Email);

        modelBuilder.Entity<ShopOwner>()
            .HasIndex(x => x.Status);

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
