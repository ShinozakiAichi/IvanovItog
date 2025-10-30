using System;
using System.Linq;
using System.Threading.Tasks;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Infrastructure;
using IvanovItog.Infrastructure.Services;
using IvanovItog.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Xunit;

namespace IvanovItog.Tests;

public class RequestServiceTests
{
    private const int NewStatusId = 1;
    private const int ClosedStatusId = 2;
    private const int InProgressStatusId = 3;

    private static RequestService CreateService(out AppDbContext context)
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        context = new AppDbContext(options);

        context.Statuses.AddRange(
            new Status { Id = NewStatusId, Name = "Новая" },
            new Status { Id = ClosedStatusId, Name = "Закрыта" },
            new Status { Id = InProgressStatusId, Name = "В работе" });
        context.Categories.Add(new Category { Id = 1, Name = "Инфраструктура" });
        context.Users.AddRange(
            new User
            {
                Id = 1,
                Login = "author",
                DisplayName = "Author",
                PasswordHash = "hash",
                Role = Role.User,
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Id = 2,
                Login = "tech",
                DisplayName = "Tech",
                PasswordHash = "hash",
                Role = Role.Tech,
                CreatedAt = DateTime.UtcNow
            });
        context.SaveChanges();

        return new RequestService(context, new RequestValidator(), Log.Logger);
    }

    private static Request CreateValidRequest(int id, int? assignedToId = null)
    {
        return new Request
        {
            Id = id,
            Title = $"Заявка {id}",
            Description = "Описание",
            CategoryId = 1,
            StatusId = NewStatusId,
            Priority = Priority.Medium,
            CreatedById = 1,
            AssignedToId = assignedToId
        };
    }

    [Fact]
    public async Task CreateAsync_ShouldPersistValidRequest()
    {
        var service = CreateService(out var context);
        var request = CreateValidRequest(0);

        var result = await service.CreateAsync(request);

        Assert.True(result.IsSuccess);
        var stored = Assert.Single(context.Requests);
        Assert.Equal(stored.Id, result.Value!.Id);
        Assert.Equal(request.Title, stored.Title);
        Assert.True((DateTime.UtcNow - stored.CreatedAt) < TimeSpan.FromMinutes(1));
    }

    [Fact]
    public async Task CreateAsync_ShouldReturnValidationErrors()
    {
        var service = CreateService(out _);
        var request = new Request
        {
            Id = 1,
            Title = string.Empty,
            Description = string.Empty,
            CategoryId = 0,
            StatusId = 0,
            Priority = Priority.Low,
            CreatedById = 0
        };

        var result = await service.CreateAsync(request);

        Assert.False(result.IsSuccess);
        Assert.NotNull(result.Error);
        Assert.Contains("Title", result.Error!, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges()
    {
        var service = CreateService(out var context);
        var request = CreateValidRequest(1);
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        request.Title = "Обновлено";
        request.Description = "Новое описание";

        var result = await service.UpdateAsync(request);

        Assert.True(result.IsSuccess);
        var updated = await context.Requests.FindAsync(request.Id);
        Assert.Equal("Обновлено", updated!.Title);
        Assert.Equal("Новое описание", updated.Description);
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveExistingRequest()
    {
        var service = CreateService(out var context);
        var request = CreateValidRequest(1);
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var result = await service.DeleteAsync(request.Id);

        Assert.True(result.IsSuccess);
        Assert.Empty(context.Requests);
    }

    [Fact]
    public async Task DeleteAsync_ShouldFailWhenRequestMissing()
    {
        var service = CreateService(out _);

        var result = await service.DeleteAsync(42);

        Assert.False(result.IsSuccess);
        Assert.Equal("RequestNotFound", result.Error);
    }

    [Fact]
    public async Task GetAsync_ShouldRespectFilters()
    {
        var service = CreateService(out var context);
        var now = DateTime.UtcNow;
        context.Requests.AddRange(
            new Request
            {
                Id = 1,
                Title = "Инцидент сеть",
                Description = "Падение сети",
                CategoryId = 1,
                StatusId = InProgressStatusId,
                Priority = Priority.High,
                CreatedById = 1,
                AssignedToId = 2,
                CreatedAt = now.AddHours(-2)
            },
            new Request
            {
                Id = 2,
                Title = "Инцидент база",
                Description = "БД недоступна",
                CategoryId = 1,
                StatusId = NewStatusId,
                Priority = Priority.Medium,
                CreatedById = 1,
                AssignedToId = null,
                CreatedAt = now.AddHours(-1)
            },
            new Request
            {
                Id = 3,
                Title = "Заявка прочее",
                Description = "Другое",
                CategoryId = 1,
                StatusId = NewStatusId,
                Priority = Priority.Low,
                CreatedById = 1,
                AssignedToId = null,
                CreatedAt = now
            });
        await context.SaveChangesAsync();

        var filter = new RequestFilter(
            CategoryId: 1,
            StatusId: null,
            Priority: null,
            CreatedFrom: now.AddHours(-3),
            CreatedTo: now,
            Search: "Инцидент",
            CreatedById: 1,
            AssignedToId: 2,
            IncludeUnassigned: true);

        var filtered = await service.GetAsync(filter);

        Assert.Equal(2, filtered.Count);
        Assert.All(filtered, r => Assert.Contains("Инцидент", r.Title));
        Assert.Contains(filtered, r => r.AssignedToId == 2);
        Assert.Contains(filtered, r => r.AssignedToId is null);
    }

    [Fact]
    public async Task AssignAsync_ShouldAssignTechnician()
    {
        var service = CreateService(out var context);
        var request = CreateValidRequest(1);
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var result = await service.AssignAsync(request.Id, 2);

        Assert.True(result.IsSuccess);
        var updated = await context.Requests.FindAsync(request.Id);
        Assert.Equal(2, updated!.AssignedToId);
    }

    [Fact]
    public async Task AssignAsync_ShouldFailWhenTechnicianMissing()
    {
        var service = CreateService(out var context);
        context.Requests.Add(CreateValidRequest(1));
        await context.SaveChangesAsync();

        var result = await service.AssignAsync(1, 99);

        Assert.False(result.IsSuccess);
        Assert.Equal("TechnicianNotFound", result.Error);
    }

    [Fact]
    public async Task CloseAsync_ShouldSetClosedAtAndStatus()
    {
        var service = CreateService(out var context);
        var request = CreateValidRequest(1);
        request.StatusId = InProgressStatusId;
        context.Requests.Add(request);
        await context.SaveChangesAsync();

        var result = await service.CloseAsync(request.Id);

        Assert.True(result.IsSuccess);
        var updated = await context.Requests.FindAsync(request.Id);
        Assert.NotNull(updated!.ClosedAt);
        Assert.Equal(ClosedStatusId, updated.StatusId);
    }
}
