using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TourService.Data;
using TourService.DTOs;
using TourService.Models;
using TourService.Services;

namespace TourService.Controllers;

[ApiController]
[Route("api/tours/me/location")]
public class TouristsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public TouristsController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [Authorize(Roles = "Tourist")]
    [HttpGet]
    public async Task<ActionResult<TouristLocationDto>> GetMyLocation()
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var location = await _context.TouristLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(tl => tl.TouristId == currentUser!.UserId);

        if (location is null)
        {
            return NotFound("Location not set.");
        }

        return Ok(ToDto(location));
    }

    [Authorize(Roles = "Tourist")]
    [HttpPut]
    public async Task<ActionResult<TouristLocationDto>> UpsertMyLocation([FromBody] TouristLocationDto dto)
    {
        if (!_currentUserService.TryGetCurrentUser(User, out var currentUser))
        {
            return Unauthorized("Missing user claims in token.");
        }

        var location = await _context.TouristLocations
            .FirstOrDefaultAsync(tl => tl.TouristId == currentUser!.UserId);

        if (location is null)
        {
            location = new TouristLocation
            {
                TouristId = currentUser!.UserId
            };
            _context.TouristLocations.Add(location);
        }

        location.Latitude = dto.Latitude;
        location.Longitude = dto.Longitude;
        location.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return Ok(ToDto(location));
    }

    private static TouristLocationDto ToDto(TouristLocation location)
    {
        return new TouristLocationDto
        {
            Latitude = location.Latitude,
            Longitude = location.Longitude,
            UpdatedAtUtc = location.UpdatedAtUtc
        };
    }
}
