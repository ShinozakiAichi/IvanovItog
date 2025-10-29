using IvanovItog.Domain.Common;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<User>> AuthenticateAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<Result<User>> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string login, CancellationToken cancellationToken = default);
    Task<Result<User>> RegisterUserAsync(string login, string displayName, string password, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<Result> UpdateUserAsync(int userId, string login, string displayName, Role role, CancellationToken cancellationToken = default);
    Task<Result> ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
    Task<Result> DeleteUserAsync(int userId, CancellationToken cancellationToken = default);
}
