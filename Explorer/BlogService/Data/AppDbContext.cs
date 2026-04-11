using BlogService.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogService.Data;

public class AppDbContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Blog>(entity =>
        {
            entity.Property(b => b.Title)
                .IsRequired()
                .HasMaxLength(120);

            entity.Property(b => b.DescriptionMarkdown)
                .IsRequired();
        });
    }
}
