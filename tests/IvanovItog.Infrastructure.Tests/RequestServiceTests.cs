using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using IvanovItog.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Xunit;

namespace IvanovItog.Infrastructure.Tests;

public class RequestServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldReturnFailure_WhenValidationFails()
    {
        // Arrange
        await using var context = CreateDbContext();
        var service = CreateService(context);
        var invalidRequest = new Request
        {
            Title = string.Empty,
            Description = string.Empty,
            CategoryId = 0,
            StatusId = 0,
            CreatedById = 0,
            Priority = Priority.Low
        };

        // Act
        var result = await service.CreateAsync(invalidRequest, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.NotEmpty(result.Error);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task AssignAsync_ShouldReturnFailure_WhenTechnicianDoesNotExist()
    {
        // Arrange
        await using var context = CreateDbContext();
        context.Requests.Add(new Request
        {
            Id = 1,
            Title = "Printer issue",
            Description = "Paper jam in printer",
            CategoryId = 1,
            StatusId = 1,
            CreatedById = 2,
            Priority = Priority.Medium,
            CreatedAt = DateTime.UtcNow
        });
        context.Users.Add(new User
        {
            Id = 5,
            Email = "user@example.com",
            PasswordHash = "hash",
            FullName = "Regular User",
            Role = Role.User
        });
        await context.SaveChangesAsync();
        var service = CreateService(context);

        // Act
        var result = await service.AssignAsync(1, technicianId: 42, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("TechnicianNotFound", result.Error);
        Assert.Null(context.Requests.Single().AssignedToId);
    }

    [Fact]
    public async Task GetAsync_ShouldReturnAssignedAndUnassigned_WhenIncludeUnassignedIsTrue()
    {
        // Arrange
        await using var context = CreateDbContext();
        context.Statuses.Add(new Status { Id = 1, Name = "Открыта" });
        context.Categories.Add(new Category { Id = 1, Name = "Hardware" });
        context.Users.AddRange(
            new User
            {
                Id = 1,
                Email = "creator@example.com",
                PasswordHash = "hash",
                FullName = "Request Creator",
                Role = Role.User
            },
            new User
            {
                Id = 10,
                Email = "tech@example.com",
                PasswordHash = "hash",
                FullName = "Tech",
                Role = Role.Tech
            });
        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Fix monitor",
                Description = "Monitor flickering",
                CategoryId = 1,
                StatusId = 1,
                CreatedById = 1,
                AssignedToId = 10,
                Priority = Priority.High,
                CreatedAt = DateTime.UtcNow
            },
            new Request
            {
                Id = 2,
                Title = "Install software",
                Description = "Install security patch",
                CategoryId = 1,
                StatusId = 1,
                CreatedById = 1,
                AssignedToId = null,
                Priority = Priority.Low,
                CreatedAt = DateTime.UtcNow.AddMinutes(-10)
            });
        await context.SaveChangesAsync();
        var service = CreateService(context);
        var filter = new RequestFilter(
            CategoryId: null,
            StatusId: null,
            Priority: null,
            CreatedFrom: null,
            CreatedTo: null,
            Search: null,
            CreatedById: null,
            AssignedToId: 10,
            IncludeUnassigned: true);

        // Act
        var result = await service.GetAsync(filter, CancellationToken.None);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, r => r.Id == 1 && r.AssignedToId == 10);
        Assert.Contains(result, r => r.Id == 2 && r.AssignedToId is null);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    private static RequestService CreateService(AppDbContext context)
    {
        var validator = new RequestValidator();
        return new RequestService(context, validator, CreateLogger());
    }

    private static ILogger CreateLogger()
    {
        return new LoggerConfiguration()
            .MinimumLevel.ControlledBy(new LoggingLevelSwitch(LogEventLevel.Verbose))
            .WriteTo.Sink(new NullSink())
            .CreateLogger();
    }

    private sealed class NullSink : ILogEventSink
    {
        public void Emit(LogEvent logEvent)
        {
        }
    }
}
