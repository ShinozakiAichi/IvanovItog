namespace IvanovItog.Domain.Dtos;

public record RequestsByStatusDto(IReadOnlyDictionary<string, int> Counts);
