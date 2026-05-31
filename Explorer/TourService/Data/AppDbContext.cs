using Microsoft.EntityFrameworkCore;
using TourService.Models;

namespace TourService.Data;

public class AppDbContext : DbContext
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<KeyPoint> KeyPoints => Set<KeyPoint>();
    public DbSet<TourTravelTime> TourTravelTimes => Set<TourTravelTime>();
    public DbSet<TouristLocation> TouristLocations => Set<TouristLocation>();
    public DbSet<TourExecution> TourExecutions => Set<TourExecution>();
    public DbSet<CompletedKeyPoint> CompletedKeyPoints => Set<CompletedKeyPoint>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tour>(entity =>
        {
            entity.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(t => t.Description)
                .IsRequired()
                .HasMaxLength(4000);

            entity.Property(t => t.Difficulty)
                .IsRequired()
                .HasMaxLength(32);

            entity.Property(t => t.AuthorUsername)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(t => t.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.Property(t => t.LengthKm);

            entity.Property(t => t.Tags)
                .HasColumnType("text[]");

            entity.HasIndex(t => t.AuthorId);

            entity.HasMany(t => t.KeyPoints)
                .WithOne(k => k.Tour)
                .HasForeignKey(k => k.TourId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(t => t.TravelTimes)
                .WithOne(tt => tt.Tour)
                .HasForeignKey(tt => tt.TourId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KeyPoint>(entity =>
        {
            entity.Property(k => k.Name)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(k => k.Description)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(k => k.ImageUrl)
                .HasMaxLength(2048);

            entity.HasIndex(k => k.TourId);
            entity.HasIndex(k => new { k.TourId, k.OrderIndex }).IsUnique();
        });

        modelBuilder.Entity<TourTravelTime>(entity =>
        {
            entity.Property(tt => tt.TransportType)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.HasIndex(tt => tt.TourId);
            entity.HasIndex(tt => new { tt.TourId, tt.TransportType }).IsUnique();
        });

        modelBuilder.Entity<TouristLocation>(entity =>
        {
            entity.HasIndex(tl => tl.TouristId).IsUnique();
        });

        modelBuilder.Entity<TourExecution>(entity =>
        {
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(32);

            entity.HasIndex(e => e.TouristId);
            entity.HasIndex(e => e.TourId);

            entity.HasMany(e => e.CompletedKeyPoints)
                .WithOne(ckp => ckp.TourExecution)
                .HasForeignKey(ckp => ckp.TourExecutionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CompletedKeyPoint>(entity =>
        {
            entity.HasIndex(ckp => ckp.TourExecutionId);
            entity.HasIndex(ckp => new { ckp.TourExecutionId, ckp.KeyPointId }).IsUnique();
        });
    }
}
