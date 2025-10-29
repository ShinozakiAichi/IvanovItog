using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Xunit;

namespace IvanovItog.Tests;

public class RatingServiceTests
{
    [Fact]
    public async Task ShouldCalculateScoreWithPenalties()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new AppDbContext(options);
        context.Users.Add(new User { Id = 1, Login = "tech", PasswordHash = "", DisplayName = "Tech", Role = Role.Tech, CreatedAt = DateTime.UtcNow });
        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Test",
                Description = "Desc",
                CategoryId = 1,
                StatusId = 1,
                Priority = Priority.High,
                CreatedById = 1,
                AssignedToId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                ClosedAt = DateTime.UtcNow
            },
            new Request
            {
                Id = 2,
                Title = "Late",
                Description = "Desc",
                CategoryId = 1,
                StatusId = 1,
                Priority = Priority.Low,
                CreatedById = 1,
                AssignedToId = 1,
                CreatedAt = DateTime.UtcNow.AddDays(-5),
                ClosedAt = DateTime.UtcNow
            });
        await context.SaveChangesAsync();

        var service = new RatingService(context, Log.Logger);
        var ratings = await service.GetRatingsAsync();
        var rating = Assert.Single(ratings);
        Assert.Equal((2 * 10) - (1 * 5) + (1 * 4), rating.Score);
    }
}
