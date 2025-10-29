using IvanovItog.Domain.Entities;
using IvanovItog.Shared;

namespace IvanovItog.Domain.Interfaces;

public interface IAuthService
{
    Task<Result<User>> AuthenticateAsync(string login, string password, CancellationToken cancellationToken = default);
    Task<Result<User>> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default);
    Task<bool> UserExistsAsync(string login, CancellationToken cancellationToken = default);
}
