using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using AuthService.Data;
using AuthService.DTOs;
using AuthService.Models;
using AuthService.Services;

namespace AuthService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IJwtService _jwtService;
    private readonly IStakeholdersClient _stakeholdersClient; // NOVO

    public AuthController(
        AppDbContext context,
        IJwtService jwtService,
        IStakeholdersClient stakeholdersClient) // NOVO
    {
        _context = context;
        _jwtService = jwtService;
        _stakeholdersClient = stakeholdersClient;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto) // NOVO: async
    {
        if (dto.Role != "Guide" && dto.Role != "Tourist")
            return BadRequest("Invalid role");

        if (_context.Users.Any(u => u.Username == dto.Username))
            return BadRequest("Username already exists");

        var user = new User
        {
            Username = dto.Username,
            Email = dto.Email,
            Role = dto.Role,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password)
        };

        _context.Users.Add(user);
        _context.SaveChanges();

        // Nakon što sa?uvamo korisnika i dobijemo njegov Id,
        // obaveštavamo Stakeholders servis da kreira prazan profil.
        // Fire-and-forget uz logovanje — registracija ne pada ako Stakeholders nije dostupan.
        await _stakeholdersClient.InitializeProfileAsync(user.Id); // NOVO

        return Ok("User created");
    }

    // Ostali endpointi ostaju nepromenjeni
    [HttpPost("login")]
    public IActionResult Login(LoginDto dto)
    {
        var user = _context.Users.FirstOrDefault(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            return Unauthorized("Invalid credentials");

        if (user.IsBlocked)
            return Unauthorized("User account is blocked");

        var token = _jwtService.GenerateToken(user);
        return Ok(new { token });
    }

    [Authorize(Roles = "Admin")]
    [HttpGet("users")]
    public IActionResult GetUsers()
    {
        var users = _context.Users
            .Select(user => new UserResponseDto
            {
                Id = user.Id,
                Username = user.Username,
                Email = user.Email,
                Role = user.Role,
                IsBlocked = user.IsBlocked
            })
            .ToList();
        return Ok(users);
    }

    [Authorize(Roles = "Admin")]
    [HttpPatch("users/{id}/block")]
    public IActionResult BlockUser(int id) => SetUserBlockedStatus(id, true);

    [Authorize(Roles = "Admin")]
    [HttpPatch("users/{id}/unblock")]
    public IActionResult UnblockUser(int id) => SetUserBlockedStatus(id, false);

    private IActionResult SetUserBlockedStatus(int id, bool isBlocked)
    {
        var user = _context.Users.FirstOrDefault(u => u.Id == id);
        if (user == null)
            return NotFound("User not found");

        if (user.Role != "Guide" && user.Role != "Tourist")
            return BadRequest("Only Guide and Tourist accounts can be blocked or unblocked");

        user.IsBlocked = isBlocked;
        _context.SaveChanges();
        return Ok(ToUserResponseDto(user));
    }

    private static UserResponseDto ToUserResponseDto(User user) => new()
    {
        Id = user.Id,
        Username = user.Username,
        Email = user.Email,
        Role = user.Role,
        IsBlocked = user.IsBlocked
    };
}