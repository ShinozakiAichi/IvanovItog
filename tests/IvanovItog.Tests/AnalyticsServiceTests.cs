using System;
using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using Microsoft.Data.Sqlite;
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

        var createdAt = new DateTime(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc);

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
                CreatedAt = createdAt
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
                CreatedAt = createdAt.AddHours(2)
            });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var stats = await service.GetRequestsByStatusAsync(
            createdAt.Date,
            createdAt.Date.AddDays(1).AddTicks(-1));

        Assert.Equal(2, stats.Count());
        var newRequestStats = Assert.Single(stats.Where(s => s.Status == newStatus.Name));
        Assert.Equal(1, newRequestStats.Count);
        var closedRequestStats = Assert.Single(stats.Where(s => s.Status == closedStatus.Name));
        Assert.Equal(1, closedRequestStats.Count);
    }

    [Fact]
    public async Task GetRequestsByStatusAsync_ShouldReturnGroupedStatisticsWithSqliteProvider()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var newStatus = new Status { Id = 1, Name = "Новая" };
        var closedStatus = new Status { Id = 2, Name = "Закрыта" };
        var category = new Category { Id = 1, Name = "Общая" };
        var author = new User
        {
            Id = 1,
            Login = "author",
            PasswordHash = string.Empty,
            DisplayName = "Author",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        context.Statuses.AddRange(newStatus, closedStatus);
        context.Categories.Add(category);
        context.Users.Add(author);

        var createdAt = new DateTime(2024, 1, 10, 10, 0, 0, DateTimeKind.Utc);

        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Первый",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.Medium,
                StatusId = newStatus.Id,
                CreatedById = author.Id,
                CreatedAt = createdAt
            },
            new Request
            {
                Id = 2,
                Title = "Второй",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.High,
                StatusId = closedStatus.Id,
                CreatedById = author.Id,
                CreatedAt = createdAt.AddHours(2)
            });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var stats = await service.GetRequestsByStatusAsync(
            createdAt.Date,
            createdAt.Date.AddDays(1).AddTicks(-1));

        Assert.Equal(2, stats.Count());
        var newRequestStats = Assert.Single(stats.Where(s => s.Status == newStatus.Name));
        Assert.Equal(1, newRequestStats.Count);
        var closedRequestStats = Assert.Single(stats.Where(s => s.Status == closedStatus.Name));
        Assert.Equal(1, closedRequestStats.Count);
    }

    [Fact]
    public async Task GetRequestsTimelineAsync_ShouldAggregateByDayWithSqliteProvider()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var status = new Status { Id = 1, Name = "Новая" };
        var category = new Category { Id = 1, Name = "Общая" };
        var author = new User
        {
            Id = 1,
            Login = "author",
            PasswordHash = string.Empty,
            DisplayName = "Author",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        context.Statuses.Add(status);
        context.Categories.Add(category);
        context.Users.Add(author);

        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Первый",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.Medium,
                StatusId = status.Id,
                CreatedById = author.Id,
                CreatedAt = new DateTime(2024, 1, 1, 10, 0, 0, DateTimeKind.Utc)
            },
            new Request
            {
                Id = 2,
                Title = "Второй",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.High,
                StatusId = status.Id,
                CreatedById = author.Id,
                CreatedAt = new DateTime(2024, 1, 1, 11, 0, 0, DateTimeKind.Utc)
            },
            new Request
            {
                Id = 3,
                Title = "Третий",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.Low,
                StatusId = status.Id,
                CreatedById = author.Id,
                CreatedAt = new DateTime(2024, 1, 2, 9, 0, 0, DateTimeKind.Utc)
            });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var timeline = await service.GetRequestsTimelineAsync(
            new DateTime(2023, 12, 31, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 1, 3, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddTicks(-1));

        Assert.Equal(2, timeline.Count);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), timeline[0].Date);
        Assert.Equal(2, timeline[0].Count);
        Assert.Equal(new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc), timeline[1].Date);
        Assert.Equal(1, timeline[1].Count);
    }

    [Fact]
    public async Task GetTechnicianLoadAsync_ShouldReturnActiveAndClosedCounts()
    {
        await using var connection = new SqliteConnection("DataSource=:memory:");
        await connection.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        await using var context = new AppDbContext(options);
        await context.Database.EnsureCreatedAsync();

        var technician = new User
        {
            Id = 1,
            Login = "tech",
            PasswordHash = string.Empty,
            DisplayName = "Tech",
            Role = Role.Tech,
            CreatedAt = DateTime.UtcNow
        };

        var author = new User
        {
            Id = 2,
            Login = "author",
            PasswordHash = string.Empty,
            DisplayName = "Author",
            Role = Role.User,
            CreatedAt = DateTime.UtcNow
        };

        var category = new Category { Id = 1, Name = "Общая" };
        var status = new Status { Id = 1, Name = "Новая" };

        context.Users.AddRange(technician, author);
        context.Categories.Add(category);
        context.Statuses.Add(status);

        var rangeStart = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var rangeEnd = new DateTime(2024, 1, 31, 0, 0, 0, DateTimeKind.Utc).AddDays(1).AddTicks(-1);

        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Активная",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.Medium,
                StatusId = status.Id,
                CreatedById = author.Id,
                AssignedToId = technician.Id,
                CreatedAt = rangeStart.AddDays(1)
            },
            new Request
            {
                Id = 2,
                Title = "Закрыта",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.High,
                StatusId = status.Id,
                CreatedById = author.Id,
                AssignedToId = technician.Id,
                CreatedAt = rangeStart.AddDays(2),
                ClosedAt = rangeStart.AddDays(3)
            },
            new Request
            {
                Id = 3,
                Title = "Вне диапазона",
                Description = "Test",
                CategoryId = category.Id,
                Priority = Priority.Low,
                StatusId = status.Id,
                CreatedById = author.Id,
                AssignedToId = technician.Id,
                CreatedAt = rangeEnd.AddDays(1),
                ClosedAt = rangeEnd.AddDays(2)
            });

        await context.SaveChangesAsync();

        var service = new AnalyticsService(context);

        var load = await service.GetTechnicianLoadAsync(rangeStart, rangeEnd);

        var technicianLoad = Assert.Single(load);
        Assert.Equal("Tech", technicianLoad.TechnicianName);
        Assert.Equal(1, technicianLoad.ActiveRequests);
        Assert.Equal(1, technicianLoad.ClosedRequests);
    }

}
