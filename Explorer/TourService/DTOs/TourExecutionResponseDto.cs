namespace TourService.DTOs;

public class TourExecutionResponseDto
{
    public int Id { get; set; }
    public int TourId { get; set; }
    public int TouristId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public DateTime? AbandonedAtUtc { get; set; }
    public DateTime LastActivityAtUtc { get; set; }
    public double InitialLatitude { get; set; }
    public double InitialLongitude { get; set; }
    public List<CompletedKeyPointDto> CompletedKeyPoints { get; set; } = new();
}

public class CompletedKeyPointDto
{
    public int KeyPointId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}

public class CheckNearbyResponseDto
{
    public bool IsNearKeyPoint { get; set; }
    public int? KeyPointId { get; set; }
    public string? KeyPointName { get; set; }
    public bool IsNewlyCompleted { get; set; }
    public bool AllKeyPointsCompleted { get; set; }
    public TourExecutionResponseDto Execution { get; set; } = null!;
}
