using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StakeholdersService.DTOs;
using StakeholdersService.Services;
using System.Security.Claims;

namespace StakeholdersService.Controllers;

[ApiController]
[Route("api/profiles")]
public class ProfileController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfileController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpPost("init")]
    public async Task<IActionResult> InitProfile([FromBody] InitProfileDto dto,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        [FromServices] IConfiguration config)
    {
        var expectedKey = config["InternalApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedKey)
            return Unauthorized("Invalid internal API key");

        var profile = await _profileService.InitializeAsync(dto.UserId);
        return Ok(profile);
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile()
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var profile = await _profileService.GetByUserIdAsync(userId.Value);
        if (profile is null) return NotFound("Profile not found");

        return Ok(profile);
    }

    [Authorize]
    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto)
    {
        var userId = GetCurrentUserId();
        if (userId is null) return Unauthorized();

        var updated = await _profileService.UpdateAsync(userId.Value, dto);
        if (updated is null) return NotFound("Profile not found");

        return Ok(updated);
    }

    private int? GetCurrentUserId()
    {
        // Čitamo custom "userId" claim koji Auth servis upisuje u token
        var userIdClaim = User.FindFirstValue("userId");
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }
}