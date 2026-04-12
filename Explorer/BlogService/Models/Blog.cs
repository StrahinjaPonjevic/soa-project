namespace BlogService.Models;

public class Blog
{
    public int Id { get; set; }
    public required string Title { get; set; }
    public required string DescriptionMarkdown { get; set; } 

    public int AuthorId { get; set; } 
    public required string AuthorUsername { get; set; } 
    public DateTime CreatedAtUtc { get; set; }
    public string[]? ImageUrls { get; set; }
}
