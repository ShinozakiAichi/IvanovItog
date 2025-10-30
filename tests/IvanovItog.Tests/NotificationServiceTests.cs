using System;
using System.Linq;
using System.Threading.Tasks;
using IvanovItog.Domain.Entities;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Xunit;

namespace IvanovItog.Tests;

public class NotificationServiceTests
{
    private static NotificationService CreateService(out AppDbContext context)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        return new NotificationService(context, Log.Logger);
    }

    [Fact]
    public async Task LogNotificationAsync_ShouldPersistNotificationAndTimestamp()
    {
        var service = CreateService(out var context);

        var notification = new Notification
        {
            Text = "Test",
            Type = "Info",
            UserId = 5,
            Timestamp = default
        };

        await service.LogNotificationAsync(notification);

        var stored = await context.Notifications.SingleAsync();
        Assert.Equal("Test", stored.Text);
        Assert.Equal("Info", stored.Type);
        Assert.Equal(5, stored.UserId);
        Assert.NotEqual(default, stored.Timestamp);
    }

    [Fact]
    public async Task GetRecentAsync_ShouldClampAndReturnLatestFirst()
    {
        var service = CreateService(out var context);
        var now = DateTime.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            context.Notifications.Add(new Notification
            {
                Text = $"Notification {i}",
                Type = "Info",
                Timestamp = now.AddMinutes(i)
            });
        }

        await context.SaveChangesAsync();

        var recentTwo = await service.GetRecentAsync(2);
        Assert.Equal(2, recentTwo.Count);
        Assert.Equal(new[] { "Notification 4", "Notification 3" }, recentTwo.Select(n => n.Text).ToArray());

        var clamped = await service.GetRecentAsync(0);
        Assert.Single(clamped);
    }
}
