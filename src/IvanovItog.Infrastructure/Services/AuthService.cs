using System.Linq;
using BCrypt.Net;
using FluentValidation;
using IvanovItog.Domain.Common;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IvanovItog.Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<User> _validator;
    private readonly ILogger _logger;

    public AuthService(AppDbContext dbContext, IValidator<User> validator, ILogger logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger.ForContext<AuthService>();
    }

    public async Task<Result<User>> AuthenticateAsync(string login, string password, CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim().ToLowerInvariant();
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Login == normalized, cancellationToken);
        if (user is null)
        {
            _logger.Warning("Authentication failed for {Login}: user not found", normalized);
            return Result<User>.Failure("UserNotFound");
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            _logger.Warning("Authentication failed for {Login}: invalid password", normalized);
            return Result<User>.Failure("InvalidCredentials");
        }

        return Result<User>.Success(user);
    }

    public async Task<Result<User>> CreateUserAsync(User user, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            _logger.Warning("User creation failed for {Login}: password required", user.Login);
            return Result<User>.Failure("PasswordRequired");
        }

        user.Login = user.Login.Trim().ToLowerInvariant();
        user.DisplayName = user.DisplayName.Trim();
        var validationResult = await _validator.ValidateAsync(user, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.Warning("User creation failed for {Login}: {Errors}", user.Login, errors);
            return Result<User>.Failure(errors);
        }

        if (await UserExistsAsync(user.Login, cancellationToken))
        {
            _logger.Warning("User creation failed: duplicate login {Login}", user.Login);
            return Result<User>.Failure("UserAlreadyExists");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
        user.CreatedAt = DateTime.UtcNow;

        await _dbContext.Users.AddAsync(user, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Created user {Login} with role {Role}", user.Login, user.Role);

        return Result<User>.Success(user);
    }

    public async Task<bool> UserExistsAsync(string login, CancellationToken cancellationToken = default)
    {
        var normalized = login.Trim().ToLowerInvariant();
        return await _dbContext.Users.AnyAsync(u => u.Login == normalized, cancellationToken);
    }

    public async Task<Result<User>> RegisterUserAsync(string login, string displayName, string password, CancellationToken cancellationToken = default)
    {
        var user = new User
        {
            Login = login,
            DisplayName = displayName,
            Role = Role.User,
            PasswordHash = string.Empty,
        };

        return await CreateUserAsync(user, password, cancellationToken);
    }

    public async Task<IReadOnlyList<User>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Users
            .OrderBy(u => u.DisplayName)
            .ThenBy(u => u.Login)
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<Result> UpdateUserAsync(int userId, string login, string displayName, Role role, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            _logger.Warning("UpdateUser failed: user {UserId} not found", userId);
            return Result.Failure("UserNotFound");
        }

        var normalizedLogin = login.Trim().ToLowerInvariant();
        if (await _dbContext.Users.AnyAsync(u => u.Id != userId && u.Login == normalizedLogin, cancellationToken))
        {
            _logger.Warning("UpdateUser failed: duplicate login {Login}", normalizedLogin);
            return Result.Failure("UserAlreadyExists");
        }

        user.Login = normalizedLogin;
        user.DisplayName = displayName.Trim();
        user.Role = role;

        var validationResult = await _validator.ValidateAsync(user, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.Warning("UpdateUser validation failed for {Login}: {Errors}", user.Login, errors);
            return Result.Failure(errors);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Updated user {Login} ({UserId})", user.Login, user.Id);
        return Result.Success();
    }

    public async Task<Result> ResetPasswordAsync(int userId, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return Result.Failure("PasswordRequired");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            _logger.Warning("ResetPassword failed: user {UserId} not found", userId);
            return Result.Failure("UserNotFound");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Password reset for user {Login} ({UserId})", user.Login, user.Id);
        return Result.Success();
    }

    public async Task<Result> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(newPassword))
        {
            return Result.Failure("PasswordRequired");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user is null)
        {
            _logger.Warning("ChangePassword failed: user {UserId} not found", userId);
            return Result.Failure("UserNotFound");
        }

        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            _logger.Warning("ChangePassword failed for {Login}: invalid current password", user.Login);
            return Result.Failure("InvalidCredentials");
        }

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Password changed for user {Login} ({UserId})", user.Login, user.Id);
        return Result.Success();
    }

    public async Task<Result> DeleteUserAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.CreatedRequests)
            .Include(u => u.AssignedRequests)
            .SingleOrDefaultAsync(u => u.Id == userId, cancellationToken);

        if (user is null)
        {
            _logger.Warning("DeleteUser failed: user {UserId} not found", userId);
            return Result.Failure("UserNotFound");
        }

        if (user.CreatedRequests.Any() || user.AssignedRequests.Any())
        {
            _logger.Warning("DeleteUser failed: user {Login} has related requests", user.Login);
            return Result.Failure("UserHasRequests");
        }

        _dbContext.Users.Remove(user);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Deleted user {Login} ({UserId})", user.Login, user.Id);
        return Result.Success();
    }
}
