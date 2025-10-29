using System;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using IvanovItog.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Xunit;

namespace IvanovItog.Tests;

public class AuthServiceTests
{
    private static AuthService CreateService(out AppDbContext context)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        var validator = new UserValidator();
        return new AuthService(context, validator, Log.Logger);
    }

    [Fact]
    public async Task ShouldRegisterUserWithUserRole()
    {
        var service = CreateService(out var context);

        var result = await service.RegisterUserAsync("NewUser", "Новый Пользователь", "Password123");

        Assert.True(result.IsSuccess);
        var user = Assert.NotNull(result.Value);
        Assert.Equal(Role.User, user.Role);
        Assert.True(BCrypt.Net.BCrypt.Verify("Password123", user.PasswordHash));
        Assert.Equal("newuser", user.Login);
        Assert.Single(context.Users);
    }

    [Fact]
    public async Task ShouldUpdateLoginAndDisplayName()
    {
        var service = CreateService(out var context);
        var existing = new User
        {
            Login = "admin",
            DisplayName = "Админ",
            Role = Role.Admin,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(existing);
        await context.SaveChangesAsync();

        var updateResult = await service.UpdateUserAsync(existing.Id, "Updated", "Обновлённый", Role.Tech);

        Assert.True(updateResult.IsSuccess);
        var updated = await context.Users.FindAsync(existing.Id);
        Assert.NotNull(updated);
        Assert.Equal("updated", updated!.Login);
        Assert.Equal("Обновлённый", updated.DisplayName);
        Assert.Equal(Role.Tech, updated.Role);
    }

    [Fact]
    public async Task ShouldRejectDuplicateLoginOnUpdate()
    {
        var service = CreateService(out var context);
        context.Users.AddRange(
            new User
            {
                Login = "admin",
                DisplayName = "Админ",
                Role = Role.Admin,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Login = "user",
                DisplayName = "Пользователь",
                Role = Role.User,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
                CreatedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var updateResult = await service.UpdateUserAsync(2, "admin", "Пользователь", Role.User);

        Assert.False(updateResult.IsSuccess);
        Assert.Equal("UserAlreadyExists", updateResult.Error);
    }

    [Fact]
    public async Task ShouldChangePasswordWhenCurrentMatches()
    {
        var service = CreateService(out var context);
        var user = new User
        {
            Login = "user",
            DisplayName = "User",
            Role = Role.User,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("oldPass"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        var result = await service.ChangePasswordAsync(user.Id, "oldPass", "newPass");

        Assert.True(result.IsSuccess);
        var updated = await context.Users.FindAsync(user.Id);
        Assert.True(BCrypt.Net.BCrypt.Verify("newPass", updated!.PasswordHash));
    }

    [Fact]
    public async Task ShouldFailToDeleteUserWithRequests()
    {
        var service = CreateService(out var context);
        var user = new User
        {
            Login = "tech",
            DisplayName = "Техник",
            Role = Role.Tech,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("123"),
            CreatedAt = DateTime.UtcNow
        };
        context.Users.Add(user);
        await context.SaveChangesAsync();

        context.Requests.Add(new Request
        {
            Title = "Test",
            Description = "Desc",
            CategoryId = 1,
            StatusId = 1,
            Priority = Priority.Medium,
            CreatedById = user.Id,
            CreatedAt = DateTime.UtcNow
        });
        await context.SaveChangesAsync();

        var result = await service.DeleteUserAsync(user.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal("UserHasRequests", result.Error);
    }
}
