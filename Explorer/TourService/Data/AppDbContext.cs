using Microsoft.EntityFrameworkCore;
using TourService.Models;

namespace TourService.Data;

public class AppDbContext : DbContext
{
    public DbSet<Tour> Tours => Set<Tour>();
    public DbSet<KeyPoint> KeyPoints => Set<KeyPoint>();
    public DbSet<TouristLocation> TouristLocations => Set<TouristLocation>();

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

            entity.Property(t => t.Tags)
                .HasColumnType("text[]");

            entity.HasIndex(t => t.AuthorId);

            entity.HasMany(t => t.KeyPoints)
                .WithOne(k => k.Tour)
                .HasForeignKey(k => k.TourId)
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

        modelBuilder.Entity<TouristLocation>(entity =>
        {
            entity.HasIndex(tl => tl.TouristId).IsUnique();
        });
    }
}
