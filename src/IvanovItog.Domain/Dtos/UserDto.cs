using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Dtos;

public record UserDto(int Id, string DisplayName, Role Role);
