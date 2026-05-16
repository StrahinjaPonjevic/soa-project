namespace TourService.Models;

public class Tour
{
    public int Id { get; set; }
    public int AuthorId { get; set; }
    public required string AuthorUsername { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public required string Difficulty { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public TourStatus Status { get; set; }
    public decimal Price { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public ICollection<KeyPoint> KeyPoints { get; set; } = new List<KeyPoint>();
}
