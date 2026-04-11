using StakeholdersService.Data;
using StakeholdersService.DTOs;
using StakeholdersService.Models;

namespace StakeholdersService.Services
{
    public class ProfileService : IProfileService
    {
        private readonly AppDbContext _context;

        public ProfileService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<ProfileResponseDto?> GetByUserIdAsync(int userId)
        {
            var profile = await _context.Profiles.FindByUserIdAsync(userId);
            return profile is null ? null : MapToResponse(profile);
        }

        public async Task<ProfileResponseDto> InitializeAsync(int userId)
        {
            // Sprečavamo duplikat ako Auth servis pošalje init dva puta
            var existing = await _context.Profiles.FindByUserIdAsync(userId);
            if (existing is not null)
                return MapToResponse(existing);

            var profile = new Profile { UserId = userId };
            _context.Profiles.Add(profile);
            await _context.SaveChangesAsync();
            return MapToResponse(profile);
        }

        public async Task<ProfileResponseDto?> UpdateAsync(int userId, UpdateProfileDto dto)
        {
            var profile = await _context.Profiles.FindByUserIdAsync(userId);
            if (profile is null) return null;

            profile.FirstName = dto.FirstName;
            profile.LastName = dto.LastName;
            profile.ProfileImageUrl = dto.ProfileImageUrl;
            profile.Biography = dto.Biography;
            profile.Motto = dto.Motto;

            await _context.SaveChangesAsync();
            return MapToResponse(profile);
        }

        private static ProfileResponseDto MapToResponse(Profile p) => new()
        {
            UserId = p.UserId,
            FirstName = p.FirstName,
            LastName = p.LastName,
            ProfileImageUrl = p.ProfileImageUrl,
            Biography = p.Biography,
            Motto = p.Motto
        };
    }

    // Extension metoda da ne ponavljamo isti LINQ upit na vise mesta
    internal static class ProfileQueryExtensions
    {
        public static async Task<Profile?> FindByUserIdAsync(
            this Microsoft.EntityFrameworkCore.DbSet<Profile> set, int userId)
            => await set.FirstOrDefaultAsync(p => p.UserId == userId);

        private static Task<Profile?> FirstOrDefaultAsync(
            this Microsoft.EntityFrameworkCore.DbSet<Profile> set,
            System.Linq.Expressions.Expression<Func<Profile, bool>> predicate)
            => Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                .FirstOrDefaultAsync(set, predicate);
    }

}
