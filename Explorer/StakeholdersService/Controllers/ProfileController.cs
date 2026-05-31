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

    // Internal endpoint — called by ApiGateway StartTour SAGA (step 2)
    [HttpPost("internal/active-tour")]
    public async Task<IActionResult> SetActiveTour(
        [FromBody] ActiveTourDto dto,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        [FromServices] IConfiguration config)
    {
        var expectedKey = config["InternalApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedKey)
            return Unauthorized("Invalid internal API key");

        var success = await _profileService.SetActiveTourExecutionAsync(dto.UserId, dto.ExecutionId);
        if (!success)
            return Conflict("Tourist already has an active tour execution recorded.");

        return Ok(new { dto.UserId, dto.ExecutionId });
    }

    // Internal endpoint — called by SAGA compensation or when tour ends
    [HttpDelete("internal/active-tour")]
    public async Task<IActionResult> ClearActiveTour(
        [FromBody] ClearActiveTourDto dto,
        [FromHeader(Name = "X-Internal-Api-Key")] string? apiKey,
        [FromServices] IConfiguration config)
    {
        var expectedKey = config["InternalApiKey"];
        if (string.IsNullOrEmpty(apiKey) || apiKey != expectedKey)
            return Unauthorized("Invalid internal API key");

        await _profileService.ClearActiveTourExecutionAsync(dto.UserId);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        // Čitamo custom "userId" claim koji Auth servis upisuje u token
        var userIdClaim = User.FindFirstValue("userId");
        return int.TryParse(userIdClaim, out var id) ? id : null;
    }
}

public record ActiveTourDto(int UserId, int ExecutionId);
public record ClearActiveTourDto(int UserId);