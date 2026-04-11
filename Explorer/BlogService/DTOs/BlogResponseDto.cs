namespace BlogService.DTOs;

public class BlogResponseDto
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string DescriptionMarkdown { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public string[]? ImageUrls { get; set; }
}
