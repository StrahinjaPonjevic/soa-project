namespace BlogService.Models;

public class BlogComment
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public int AuthorId { get; set; }
    public required string AuthorUsername { get; set; }
    public required string Text { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
