namespace AuthService.DTOs;

public class UserResponseDto
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Email { get; set; }
    public string Role { get; set; }
    public bool IsBlocked { get; set; }
}
