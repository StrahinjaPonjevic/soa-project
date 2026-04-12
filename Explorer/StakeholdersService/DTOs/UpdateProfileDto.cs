namespace StakeholdersService.DTOs
{
    public class UpdateProfileDto
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfileImageUrl { get; set; }
        public string? Biography { get; set; }
        public string? Motto { get; set; }
    }
}
