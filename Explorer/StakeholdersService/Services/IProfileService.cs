using StakeholdersService.DTOs;

namespace StakeholdersService.Services
{
    public interface IProfileService
    {
        Task<ProfileResponseDto?> GetByUserIdAsync(int userId);
        Task<ProfileResponseDto> InitializeAsync(int userId);
        Task<ProfileResponseDto?> UpdateAsync(int userId, UpdateProfileDto dto);
        Task<bool> SetActiveTourExecutionAsync(int userId, int executionId);
        Task ClearActiveTourExecutionAsync(int userId);
    }

}
