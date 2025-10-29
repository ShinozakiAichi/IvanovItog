using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace IvanovItog.Infrastructure.Services;

public class LookupService : ILookupService
{
    private static readonly IReadOnlyCollection<Priority> Priorities = Enum.GetValues<Priority>();

    private readonly AppDbContext _dbContext;

    public LookupService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<Category>> GetCategoriesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Categories
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<Status>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.Statuses
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    public IReadOnlyCollection<Priority> GetPriorities() => Priorities;
}
