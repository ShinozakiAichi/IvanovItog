namespace IvanovItog.Shared.Dtos;

public record RequestsByStatusDto(IReadOnlyDictionary<string, int> Counts);
