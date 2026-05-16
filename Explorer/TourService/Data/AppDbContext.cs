using Microsoft.EntityFrameworkCore;
using TourService.Models;

namespace TourService.Data;

public class AppDbContext : DbContext
{
    public DbSet<Tour> Tours => Set<Tour>();

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
        });
    }
}
