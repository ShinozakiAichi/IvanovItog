using IvanovItog.Domain.Enums;

namespace IvanovItog.Shared.Dtos;

public record UserDto(int Id, string DisplayName, Role Role);
