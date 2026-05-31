using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourService.Data;
using TourService.DTOs;
using TourService.Services;

namespace TourService.Controllers;

[ApiController]
[Route("api/tours/executions")]
[Authorize(Roles = "Tourist")]
public class TourExecutionController : ControllerBase
{
    private readonly ICurrentUserService _currentUserService;
    private readonly ITourExecutionService _tourExecutionService;
    private readonly AppDbContext _context;

    public TourExecutionController(
        ICurrentUserService currentUserService,
        ITourExecutionService tourExecutionService,
        AppDbContext context)
    {
        _currentUserService = currentUserService;
        _tourExecutionService = tourExecutionService;
        _context = context;
    }

    // Returns ALL key points for the tourist's active execution — bypasses the
    // 1-keypoint restriction that applies on the public tour catalog.
    [HttpGet("{executionId:int}/keypoints")]
    public async Task<IActionResult> GetKeyPoints(int executionId, CancellationToken ct)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
            return Unauthorized();

        var execution = await _tourExecutionService.GetByIdAsync(executionId, currentUser!.UserId, ct);
        if (execution is null) return NotFound("Tour execution not found.");

        var keyPoints = await _context.KeyPoints
            .AsNoTracking()
            .Where(kp => kp.TourId == execution.TourId)
            .OrderBy(kp => kp.OrderIndex)
            .ThenBy(kp => kp.Id)
            .Select(kp => new KeyPointResponseDto
            {
                Id = kp.Id,
                TourId = kp.TourId,
                Name = kp.Name,
                Description = kp.Description,
                Latitude = kp.Latitude,
                Longitude = kp.Longitude,
                ImageUrl = kp.ImageUrl,
                OrderIndex = kp.OrderIndex
            })
            .ToListAsync(ct);

        return Ok(keyPoints);
    }

    [HttpGet("active")]
    public async Task<IActionResult> GetActive(CancellationToken ct)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
            return Unauthorized();

        var execution = await _tourExecutionService.GetActiveForTouristAsync(currentUser!.UserId, ct);
        if (execution is null) return NotFound("No active tour execution.");

        return Ok(TourRpcController.ToExecutionDto(execution));
    }

    [HttpGet("{executionId:int}")]
    public async Task<IActionResult> GetById(int executionId, CancellationToken ct)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
            return Unauthorized();

        var execution = await _tourExecutionService.GetByIdAsync(executionId, currentUser!.UserId, ct);
        if (execution is null) return NotFound("Tour execution not found.");

        return Ok(TourRpcController.ToExecutionDto(execution));
    }

    [HttpPost("{executionId:int}/complete")]
    public async Task<IActionResult> Complete(int executionId, CancellationToken ct)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
            return Unauthorized();

        try
        {
            var execution = await _tourExecutionService.CompleteAsync(executionId, currentUser!.UserId, ct);
            return Ok(TourRpcController.ToExecutionDto(execution));
        }
        catch (TourOperationException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }

    [HttpPost("{executionId:int}/abandon")]
    public async Task<IActionResult> Abandon(int executionId, CancellationToken ct)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
            return Unauthorized();

        try
        {
            var execution = await _tourExecutionService.AbandonAsync(executionId, currentUser!.UserId, ct);
            return Ok(TourRpcController.ToExecutionDto(execution));
        }
        catch (TourOperationException ex)
        {
            return StatusCode(ex.StatusCode, new { message = ex.Message });
        }
    }
}
