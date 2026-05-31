namespace StakeholdersService.Models;

public class Profile
{
    public int Id { get; set; }
    public int UserId { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;

    public string? ProfileImageUrl { get; set; }

    public string? Biography { get; set; }

    public string? Motto { get; set; }

    // Set by StartTour SAGA; cleared when execution ends
    public int? ActiveTourExecutionId { get; set; }
}