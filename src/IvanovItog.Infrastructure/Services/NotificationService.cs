using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IvanovItog.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public NotificationService(AppDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger.ForContext<NotificationService>();
    }

    public async Task LogNotificationAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        notification.Timestamp = notification.Timestamp == default ? DateTime.UtcNow : notification.Timestamp;
        await _dbContext.Notifications.AddAsync(notification, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Logged notification for user {UserId}: {Type}", notification.UserId, notification.Type);
    }

    public async Task<IReadOnlyCollection<Notification>> GetRecentAsync(int take, CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take, 1, 100);
        return await _dbContext.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.Timestamp)
            .Take(take)
            .ToListAsync(cancellationToken);
    }
}
