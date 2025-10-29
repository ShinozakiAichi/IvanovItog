using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IvanovItog.Infrastructure.Seed;

public static class SeedData
{
    private static readonly (string Name, Role Role)[] DefaultUsers =
    {
        ("admin", Role.Admin),
        ("tech", Role.Tech),
        ("user", Role.User)
    };

    private static readonly string[] DefaultCategories = { "ПО", "Оборудование", "Сеть", "Прочее" };
    private static readonly string[] DefaultStatuses = { "Новая", "В работе", "Закрыта", "Отменена" };

    public static async Task InitializeAsync(AppDbContext context, CancellationToken ct = default)
    {
        try
        {
            await context.Database.MigrateAsync(ct);
        }
        catch
        {
            await context.Database.EnsureCreatedAsync(ct);
        }

        if (!await context.Categories.AnyAsync(ct))
        {
            context.Categories.AddRange(DefaultCategories.Select(n => new Category { Name = n }));
        }

        if (!await context.Statuses.AnyAsync(ct))
        {
            context.Statuses.AddRange(DefaultStatuses.Select(n => new Status { Name = n }));
        }

        if (!await context.Users.AnyAsync(ct))
        {
            context.Users.AddRange(DefaultUsers.Select(u => new User
            {
                Login = u.Name,
                DisplayName = u.Name,
                Role = u.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CreatedAt = DateTime.UtcNow
            }));
        }

        await context.SaveChangesAsync(ct);
    }
}
