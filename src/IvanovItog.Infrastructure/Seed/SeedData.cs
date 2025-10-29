using System.Linq;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace IvanovItog.Infrastructure.Seed;

public static class SeedData
{
    private record RequestSeed(
        string Title,
        string Description,
        string Category,
        Priority Priority,
        string Status,
        string CreatedByLogin,
        string? AssignedToLogin,
        DateTime CreatedAt,
        DateTime? ClosedAt);

    private static readonly (string Login, string DisplayName, Role Role)[] DefaultUsers =
    {
        ("admin", "Администратор системы", Role.Admin),
        ("tech.ivanov", "Иванов Иван Иванович", Role.Tech),
        ("tech.petrov", "Петров Пётр Петрович", Role.Tech),
        ("tech.sidorova", "Сидорова Анна Сергеевна", Role.Tech),
        ("user.mironova", "Миронова Елена Павловна", Role.User)
    };

    private static readonly RequestSeed[] DefaultRequests =
    {
        new(
            "Сбой авторизации",
            "Пользователь не может войти в учебный портал после сброса пароля.",
            "ПО",
            Priority.High,
            "В работе",
            "user.mironova",
            "tech.ivanov",
            new DateTime(2025, 9, 1, 8, 30, 0, DateTimeKind.Utc),
            null),
        new(
            "Не работает проектор",
            "В аудитории А-201 проектор не включается, требуется диагностика.",
            "Оборудование",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 9, 3, 10, 15, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 4, 14, 40, 0, DateTimeKind.Utc)),
        new(
            "Проблема с Wi-Fi",
            "В корпусе Б нестабильный сигнал, преподаватели жалуются на обрывы.",
            "Сеть",
            Priority.Medium,
            "В работе",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 7, 7, 50, 0, DateTimeKind.Utc),
            null),
        new(
            "Обновление ПО",
            "Нужно обновить ПО для лаборатории робототехники до последней версии.",
            "ПО",
            Priority.Medium,
            "Новая",
            "admin",
            null,
            new DateTime(2025, 9, 10, 9, 0, 0, DateTimeKind.Utc),
            null),
        new(
            "Замена батарей в микрофонах",
            "Плановая замена батарей в беспроводных микрофонах конференц-зала.",
            "Оборудование",
            Priority.Low,
            "Закрыта",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 12, 11, 20, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 13, 9, 45, 0, DateTimeKind.Utc)),
        new(
            "Добавить пользователя в LMS",
            "Нужно создать профиль нового преподавателя в LMS и настроить доступы.",
            "ПО",
            Priority.Low,
            "Закрыта",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 9, 15, 13, 5, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 15, 16, 30, 0, DateTimeKind.Utc)),
        new(
            "Заявка на новую точку доступа",
            "Нужно добавить точку доступа Wi-Fi в библиотеке для улучшения покрытия.",
            "Сеть",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 20, 8, 10, 0, DateTimeKind.Utc),
            null),
        new(
            "Не печатает МФУ",
            "МФУ в кабинете 305 показывает ошибку бумаги, требуется осмотр.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 9, 24, 12, 25, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 24, 15, 5, 0, DateTimeKind.Utc)),
        new(
            "Монтаж дополнительной розетки",
            "В аудитории Б-104 необходимо установить дополнительную сетевую розетку.",
            "Сеть",
            Priority.Medium,
            "Новая",
            "admin",
            null,
            new DateTime(2025, 10, 1, 9, 35, 0, DateTimeKind.Utc),
            null),
        new(
            "Настройка почтового клиента",
            "У преподавателя не синхронизируются письма в Outlook, требуется помощь.",
            "ПО",
            Priority.Low,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 10, 5, 10, 40, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 5, 12, 0, 0, DateTimeKind.Utc)),
        new(
            "Регистрация датчиков температуры",
            "Необходимо зарегистрировать новые датчики температуры в системе мониторинга.",
            "Прочее",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 12, 14, 0, 0, DateTimeKind.Utc),
            null),
        new(
            "Отсутствует звук в Zoom",
            "Студенты не слышат лектора при трансляции, возможно, отключён микшер.",
            "ПО",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 10, 18, 8, 55, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 18, 9, 40, 0, DateTimeKind.Utc)),
        new(
            "Плановое обслуживание серверов",
            "Нужно провести обновление и перезагрузку серверов учебного центра.",
            "Прочее",
            Priority.High,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 25, 21, 0, 0, DateTimeKind.Utc),
            null)
    };

    private static readonly string[] DefaultCategories = { "ПО", "Оборудование", "Сеть", "Прочее" };
    private static readonly string[] DefaultStatuses = { "Новая", "В работе", "Закрыта", "Отменена" };

    public static async Task InitializeAsync(AppDbContext context, CancellationToken ct = default)
    {
        await context.Database.EnsureCreatedAsync(ct);

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
                Login = u.Login,
                DisplayName = u.DisplayName,
                Role = u.Role,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("123456"),
                CreatedAt = DateTime.UtcNow
            }));
        }

        await context.SaveChangesAsync(ct);

        if (!await context.Requests.AnyAsync(ct))
        {
            var categories = await context.Categories.ToDictionaryAsync(c => c.Name, ct);
            var statuses = await context.Statuses.ToDictionaryAsync(s => s.Name, ct);
            var users = await context.Users.ToDictionaryAsync(u => u.Login, ct);

            var requests = DefaultRequests.Select(seed => new Request
            {
                Title = seed.Title,
                Description = seed.Description,
                CategoryId = categories[seed.Category].Id,
                Priority = seed.Priority,
                StatusId = statuses[seed.Status].Id,
                CreatedById = users[seed.CreatedByLogin].Id,
                AssignedToId = seed.AssignedToLogin is null ? null : users[seed.AssignedToLogin].Id,
                CreatedAt = seed.CreatedAt,
                ClosedAt = seed.ClosedAt
            });

            context.Requests.AddRange(requests);
        }

        await context.SaveChangesAsync(ct);
    }
}
