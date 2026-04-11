using Microsoft.EntityFrameworkCore;
using StakeholdersService.Models;

namespace StakeholdersService.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Profile> Profiles => Set<Profile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Profile>(entity =>
        {
            // UserId mora biti jedinstven — jedan korisnik, jedan profil
            entity.HasIndex(p => p.UserId).IsUnique();
        });
    }
}