using System;
using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace IvanovItog.Tests;

public class LookupServiceTests
{
    private static LookupService CreateService(out AppDbContext context)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);
        return new LookupService(context);
    }

    [Fact]
    public async Task GetCategoriesAsync_ShouldReturnCategoriesOrderedByName()
    {
        var service = CreateService(out var context);
        context.Categories.AddRange(
            new Category { Id = 2, Name = "Система" },
            new Category { Id = 1, Name = "База данных" },
            new Category { Id = 3, Name = "Аналитика" });
        await context.SaveChangesAsync();

        var categories = await service.GetCategoriesAsync();

        Assert.Equal(new[] { "Аналитика", "База данных", "Система" }, categories.Select(c => c.Name).ToArray());
    }

    [Fact]
    public async Task GetStatusesAsync_ShouldReturnStatusesOrderedByName()
    {
        var service = CreateService(out var context);
        context.Statuses.AddRange(
            new Status { Id = 2, Name = "В работе" },
            new Status { Id = 1, Name = "Новая" },
            new Status { Id = 3, Name = "Закрыта" });
        await context.SaveChangesAsync();

        var statuses = await service.GetStatusesAsync();

        Assert.Equal(new[] { "В работе", "Закрыта", "Новая" }.OrderBy(n => n).ToArray(), statuses.Select(s => s.Name).ToArray());
    }

    [Fact]
    public void GetPriorities_ShouldReturnAllEnumValues()
    {
        var service = CreateService(out var context);

        var priorities = service.GetPriorities();

        Assert.Equal(Enum.GetValues<Priority>(), priorities);
    }
}
