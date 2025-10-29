namespace IvanovItog.Domain.Dtos;

public record TechnicianRatingDto(
    int TechnicianId,
    string TechnicianName,
    int ClosedCount,
    int OverdueCount,
    int HighPriorityCount,
    int Score
);
