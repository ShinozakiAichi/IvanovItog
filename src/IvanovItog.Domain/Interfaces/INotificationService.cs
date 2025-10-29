using IvanovItog.Domain.Entities;

namespace IvanovItog.Domain.Interfaces;

public interface INotificationService
{
    Task LogNotificationAsync(Notification notification, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Notification>> GetRecentAsync(int take, CancellationToken cancellationToken = default);
}
