namespace BlogService.Models;

public class BlogLike
{
    public int Id { get; set; }
    public int BlogId { get; set; }
    public Blog Blog { get; set; } = null!;
    public int UserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
