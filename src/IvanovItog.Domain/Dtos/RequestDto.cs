using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Dtos;

public record RequestDto(
    int Id,
    string Title,
    string Description,
    string Category,
    Priority Priority,
    string Status,
    string CreatedBy,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime? ClosedAt
);
