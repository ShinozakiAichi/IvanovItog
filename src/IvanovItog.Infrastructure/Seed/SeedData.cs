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
            null
        ),
        new(
            "Перенастройка принтера кафедры",
            "На кафедре ИТ принтер сбрасывает очередь заданий, требуется настройка драйверов.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 1, 9, 40, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 14, 11, 20, 0, DateTimeKind.Utc)
        ),
        new(
            "Диагностика сервера практики",
            "Учебный сервер перестал отвечать, подозрение на сбой RAID контроллера.",
            "Прочее",
            Priority.High,
            "Закрыта",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 2, 9, 40, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 10, 16, 15, 0, DateTimeKind.Utc)
        ),
        new(
            "Синхронизация календарей",
            "У сотрудников деканата не синхронизируются календари Outlook с Exchange.",
            "ПО",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 2, 12, 5, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 2, 15, 45, 0, DateTimeKind.Utc)
        ),
        new(
            "Не работает проектор",
            "В аудитории А-201 проектор не включается, требуется диагностика.",
            "Оборудование",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 9, 3, 10, 15, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 4, 14, 40, 0, DateTimeKind.Utc)
        ),
        new(
            "Настройка доступа к 1С",
            "Бухгалтерия не может подключиться к 1С после обновления сертификата.",
            "ПО",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 9, 3, 13, 10, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Обновление ПО лаборатории",
            "Необходимо обновить ПО роботов для лаборатории мехатроники.",
            "ПО",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 4, 9, 0, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Сбой Wi-Fi в библиотеке",
            "Сеть Eduroam обрывается каждые 5 минут в читальном зале.",
            "Сеть",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 4, 11, 35, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 5, 10, 10, 0, DateTimeKind.Utc)
        ),
        new(
            "Замена дисков в NAS",
            "На сетевом хранилище появились ошибки SMART, требуется плановая замена.",
            "Прочее",
            Priority.High,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 5, 8, 15, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Регистрация новых пропусков",
            "Преподаватели кафедры менеджмента не могут оформить электронные пропуска.",
            "Прочее",
            Priority.Low,
            "Закрыта",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 9, 5, 13, 45, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 25, 9, 45, 0, DateTimeKind.Utc)
        ),
        new(
            "Переустановка ОС",
            "Учебный ПК в аудитории Б-104 не загружается после обновления BIOS.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.ivanov",
            new DateTime(2025, 9, 6, 7, 50, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 6, 16, 20, 0, DateTimeKind.Utc)
        ),
        new(
            "Резервное копирование кафедры",
            "Нужно настроить резервное копирование файлов кафедры экономики на облако.",
            "Прочее",
            Priority.Low,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 6, 12, 30, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Проблема с Wi-Fi",
            "В корпусе Б нестабильный сигнал, преподаватели жалуются на обрывы.",
            "Сеть",
            Priority.Medium,
            "В работе",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 7, 7, 50, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Задержка публикации результатов",
            "Сервис публикации результатов экзаменов зависает при выгрузке.",
            "ПО",
            Priority.High,
            "Закрыта",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 7, 9, 40, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 8, 13, 25, 0, DateTimeKind.Utc)
        ),
        new(
            "Установка графического драйвера",
            "ПК кафедры дизайна перестал видеть второй монитор после обновления.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 9, 8, 8, 55, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 8, 15, 35, 0, DateTimeKind.Utc)
        ),
        new(
            "Планирование замены проекторов",
            "Нужно подготовить смету на замену проекторов в аудиториях третьего корпуса.",
            "Оборудование",
            Priority.Low,
            "В работе",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 9, 8, 10, 5, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Сбой печати дипломов",
            "Принтер при печати дипломов оставляет полосы, требуется чистка узлов.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 9, 7, 45, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 9, 12, 20, 0, DateTimeKind.Utc)
        ),
        new(
            "Заявка на новую точку доступа",
            "Нужно добавить точку доступа Wi-Fi в библиотеке для улучшения покрытия.",
            "Сеть",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 9, 11, 10, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Обновление расписания",
            "Сервис расписания не показывает новые группы, необходимо проверить синхронизацию.",
            "ПО",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 10, 8, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 10, 12, 55, 0, DateTimeKind.Utc)
        ),
        new(
            "Обследование стойки СКС",
            "В серверной обнаружены ошибки на патч-панели, требуется тестирование линий.",
            "Сеть",
            Priority.Medium,
            "Отменена",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 10, 13, 45, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Профилактика источников питания",
            "Запланировать замену батарей ИБП в лаборатории электроники.",
            "Оборудование",
            Priority.Low,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 11, 9, 0, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Запрос на лицензии MATLAB",
            "Необходимо продлить лицензии MATLAB для кафедры автоматики.",
            "ПО",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 11, 11, 25, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 12, 10, 40, 0, DateTimeKind.Utc)
        ),
        new(
            "Замена батарей в микрофонах",
            "Плановая замена батарей в беспроводных микрофонах конференц-зала.",
            "Оборудование",
            Priority.Low,
            "Закрыта",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 12, 11, 20, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 13, 9, 45, 0, DateTimeKind.Utc)
        ),
        new(
            "Настройка отказоустойчивости VPN",
            "Студенты не могут подключиться к VPN при пиковых нагрузках.",
            "Сеть",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 9, 12, 14, 30, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Создание учётной записи преподавателя",
            "Нужно создать профиль нового преподавателя в LMS и настроить доступы.",
            "ПО",
            Priority.Low,
            "Закрыта",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 9, 13, 9, 5, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 28, 12, 20, 0, DateTimeKind.Utc)
        ),
        new(
            "Подключение нового принтера",
            "В кабинете 217 установили МФУ, нужно добавить его в систему печати.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 9, 13, 11, 15, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 24, 8, 50, 0, DateTimeKind.Utc)
        ),
        new(
            "Заявка на прокладку кабеля",
            "Необходимо протянуть витую пару до новой лаборатории прототипирования.",
            "Сеть",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 9, 14, 9, 30, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Не открывается отчёт PowerBI",
            "Отчёт по нагрузке на аудитории перестал обновляться и выдаёт ошибку доступа.",
            "ПО",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.ivanov",
            new DateTime(2025, 9, 14, 12, 5, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 15, 9, 40, 0, DateTimeKind.Utc)
        ),
        new(
            "Обновление регламента ИБ",
            "Юридический отдел запросил обновление регламента информационной безопасности.",
            "Прочее",
            Priority.Low,
            "Новая",
            "admin",
            null,
            new DateTime(2025, 9, 15, 8, 20, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка Zoom для конференции",
            "Нужно подготовить Zoom-вебинар для международной конференции.",
            "ПО",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 9, 15, 10, 55, 0, DateTimeKind.Utc),
            new DateTime(2025, 9, 15, 14, 35, 0, DateTimeKind.Utc)
        ),
        new(
            "Монтаж дополнительной розетки",
            "В аудитории Б-104 необходимо установить дополнительную сетевую розетку.",
            "Сеть",
            Priority.Medium,
            "Новая",
            "admin",
            null,
            new DateTime(2025, 10, 1, 9, 35, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка почтового клиента",
            "У преподавателя не синхронизируются письма в Outlook, требуется помощь.",
            "ПО",
            Priority.Low,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 10, 1, 13, 10, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 1, 15, 0, 0, DateTimeKind.Utc)
        ),
        new(
            "Перенос сервиса видеозаписей",
            "Необходимо перенести сервис видеозаписей на новый кластер.",
            "ПО",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 3, 9, 30, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Проверка RFID-турникетов",
            "Турникеты в корпусе Д периодически не открываются.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.petrov",
            new DateTime(2025, 10, 3, 11, 45, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 4, 13, 5, 0, DateTimeKind.Utc)
        ),
        new(
            "Восстановление кластера Kubernetes",
            "Один из узлов кластера Kubernetes вышел из строя, требуется восстановление.",
            "ПО",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 5, 9, 10, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка мультимедийной аудитории",
            "Новая аудитория требует проверки аудиосистемы и камер.",
            "Оборудование",
            Priority.Medium,
            "Закрыта",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 5, 14, 20, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 7, 16, 30, 0, DateTimeKind.Utc)
        ),
        new(
            "Оптимизация системы опросов",
            "Студенты жалуются на медленную работу сервиса опросов.",
            "ПО",
            Priority.Medium,
            "Закрыта",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 7, 8, 50, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 9, 15, 10, 0, DateTimeKind.Utc)
        ),
        new(
            "Ревизия сетевых политик",
            "Нужно пересмотреть правила фаервола для лабораторий.",
            "Сеть",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 7, 13, 25, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка прокторинга",
            "Сервис прокторинга не запускает камеру у студентов.",
            "ПО",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.ivanov",
            new DateTime(2025, 10, 9, 8, 10, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 9, 13, 55, 0, DateTimeKind.Utc)
        ),
        new(
            "Ремонт интерактивной панели",
            "Интерактивная панель в аудитории 403 не реагирует на касания.",
            "Оборудование",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 9, 12, 20, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка чат-бота приёмной комиссии",
            "Чат-бот перестал отвечать на сообщения поступающих.",
            "ПО",
            Priority.High,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 10, 12, 11, 0, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 20, 9, 50, 0, DateTimeKind.Utc)
        ),
        new(
            "Переучёт складской техники",
            "Нужно провести переучёт складского оборудования и обновить карточки.",
            "Прочее",
            Priority.Low,
            "В работе",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 10, 12, 14, 20, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Планирование закупки ноутбуков",
            "Кафедре математики требуется 15 новых ноутбуков.",
            "Оборудование",
            Priority.Low,
            "Новая",
            "admin",
            null,
            new DateTime(2025, 10, 15, 9, 15, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Подготовка лаборатории AR",
            "Нужно обновить ПО и прошивки очков дополненной реальности.",
            "ПО",
            Priority.High,
            "В работе",
            "admin",
            "tech.sidorova",
            new DateTime(2025, 10, 15, 11, 40, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Анализ логов безопасности",
            "Необходимо проверить подозрительную активность в логах SIEM.",
            "Прочее",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 18, 8, 35, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Обновление методических материалов",
            "Преподаватели попросили обновить материалы на образовательном портале.",
            "ПО",
            Priority.Medium,
            "Закрыта",
            "user.mironova",
            "tech.sidorova",
            new DateTime(2025, 10, 18, 12, 30, 0, DateTimeKind.Utc),
            new DateTime(2025, 10, 21, 10, 10, 0, DateTimeKind.Utc)
        ),
        new(
            "Аудит сетевого оборудования",
            "Нужно проверить конфигурации коммутаторов третьего корпуса.",
            "Сеть",
            Priority.Medium,
            "В работе",
            "admin",
            "tech.petrov",
            new DateTime(2025, 10, 24, 8, 20, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Обучение новых техников",
            "Организовать обучение по стандартам обслуживания.",
            "Прочее",
            Priority.Low,
            "Отменена",
            "admin",
            null,
            new DateTime(2025, 10, 24, 10, 45, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Расширение дискового пула",
            "Заканчивается место на файловом сервере, нужно расширить пул.",
            "Прочее",
            Priority.High,
            "В работе",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 30, 9, 0, 0, DateTimeKind.Utc),
            null
        ),
        new(
            "Настройка лаборатории ИИ",
            "Необходимо установить новые библиотеки на GPU-сервер лаборатории.",
            "ПО",
            Priority.High,
            "Закрыта",
            "admin",
            "tech.ivanov",
            new DateTime(2025, 10, 30, 11, 5, 0, DateTimeKind.Utc),
            new DateTime(2025, 11, 6, 18, 20, 0, DateTimeKind.Utc)
        ),
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
