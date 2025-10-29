using System.Linq;
using BCrypt.Net;
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

    public static async Task InitializeAsync(AppDbContext context, CancellationToken cancellationToken = default)
    {
        var migrations = context.Database.GetMigrations();

        if (migrations.Any())
        {
            await context.Database.MigrateAsync(cancellationToken);
        }
        else
        {
            await context.Database.EnsureCreatedAsync(cancellationToken);
        }

        if (!await context.Categories.AnyAsync(cancellationToken))
        {
            foreach (var category in DefaultCategories)
            {
                context.Categories.Add(new Category { Name = category });
            }
        }

        if (!await context.Statuses.AnyAsync(cancellationToken))
        {
            foreach (var status in DefaultStatuses)
            {
                context.Statuses.Add(new Status { Name = status });
            }
        }

        if (!await context.Users.AnyAsync(cancellationToken))
        {
            foreach (var user in DefaultUsers)
            {
                context.Users.Add(new User
                {
                    Login = user.Name,
                    DisplayName = user.Name,
                    Role = user.Role,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
