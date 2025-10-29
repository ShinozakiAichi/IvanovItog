using IvanovItog.Domain.Enums;

namespace IvanovItog.Shared.Dtos;

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
