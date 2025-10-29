using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IvanovItog.Tests;

public class AnalyticsServiceTests
{
    [Fact]
    public async Task GetRequestsByStatusAsync_ShouldReturnGroupedStatistics()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);

        var newStatus = new Status { Id = 1, Name = "Новая" };
        var closedStatus = new Status { Id = 2, Name = "Закрыта" };

        context.Statuses.AddRange(newStatus, closedStatus);
        context.Users.Add(new User
        {
            Id = 1,
            Login = "author",
            PasswordHash = string.Empty,
            DisplayName = "Author",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        });

        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Первый",
                Description = "Test",
                CategoryId = 1,
                Priority = Priority.Medium,
                StatusId = newStatus.Id,
                Status = newStatus,
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow
            },
            new Request
            {
                Id = 2,
                Title = "Второй",
                Description = "Test",
                CategoryId = 1,
                Priority = Priority.High,
                StatusId = closedStatus.Id,
                Status = closedStatus,
                CreatedById = 1,
                CreatedAt = DateTime.UtcNow
            });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var stats = await service.GetRequestsByStatusAsync();

        Assert.Equal(2, stats.Count());
        var newRequestStats = Assert.Single(stats.Where(s => s.Status == newStatus.Name));
        Assert.Equal(1, newRequestStats.Count);
        var closedRequestStats = Assert.Single(stats.Where(s => s.Status == closedStatus.Name));
        Assert.Equal(1, closedRequestStats.Count);
    }
}
