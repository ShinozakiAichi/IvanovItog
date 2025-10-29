using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Interfaces;

public interface ILookupService
{
    Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<Status>> GetStatusesAsync(CancellationToken cancellationToken = default);
    IReadOnlyCollection<Priority> GetPriorities();
}
