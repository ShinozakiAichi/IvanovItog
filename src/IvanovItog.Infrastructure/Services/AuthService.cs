using System.Linq;
using BCrypt.Net;
using FluentValidation;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared;
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
        user.Login = user.Login.Trim().ToLowerInvariant();
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
}
