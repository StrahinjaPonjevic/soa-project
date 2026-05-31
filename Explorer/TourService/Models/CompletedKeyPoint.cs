namespace TourService.Models;

public class CompletedKeyPoint
{
    public int Id { get; set; }
    public int TourExecutionId { get; set; }
    public int KeyPointId { get; set; }
    public DateTime CompletedAtUtc { get; set; }

    public TourExecution TourExecution { get; set; } = null!;
}
