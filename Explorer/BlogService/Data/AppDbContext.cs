using BlogService.Models;
using Microsoft.EntityFrameworkCore;

namespace BlogService.Data;

public class AppDbContext : DbContext
{
    public DbSet<Blog> Blogs => Set<Blog>();
    public DbSet<BlogComment> BlogComments => Set<BlogComment>();
    public DbSet<BlogLike> BlogLikes => Set<BlogLike>();

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

            entity.HasMany(b => b.Comments)
                .WithOne(c => c.Blog)
                .HasForeignKey(c => c.BlogId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(b => b.Likes)
                .WithOne(l => l.Blog)
                .HasForeignKey(l => l.BlogId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<BlogComment>(entity =>
        {
            entity.Property(c => c.AuthorUsername)
                .IsRequired()
                .HasMaxLength(80);

            entity.Property(c => c.Text)
                .IsRequired()
                .HasMaxLength(2000);

            entity.HasIndex(c => c.BlogId);
        });

        modelBuilder.Entity<BlogLike>(entity =>
        {
            entity.HasIndex(l => l.BlogId);
            entity.HasIndex(l => new { l.BlogId, l.UserId }).IsUnique();
        });
    }
}
