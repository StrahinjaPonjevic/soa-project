namespace TourService.Models;

public class TourExecution
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public int TouristId { get; set; }
    public TourExecutionStatus Status { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? AbandonedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }

    public Tour Tour { get; set; } = null!;
    public ICollection<CompletedKeyPoint> CompletedKeyPoints { get; set; } = new List<CompletedKeyPoint>();
}
