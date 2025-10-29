using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace IvanovItog.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _dbContext;

    public AnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<RequestsByStatusDto> GetRequestsByStatusAsync(CancellationToken cancellationToken = default)
    {
        var counts = await _dbContext.Requests
            .Include(r => r.Status)
            .GroupBy(r => r.Status != null ? r.Status.Name : "Не задан")
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Status, x => x.Count, cancellationToken);

        return new RequestsByStatusDto(counts);
    }

    public async Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var timeline = await _dbContext.Requests
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new RequestsTimelinePointDto(g.Key, g.Count()))
            .OrderBy(p => p.Date)
            .ToListAsync(cancellationToken);

        return timeline;
    }

    public async Task<IReadOnlyCollection<TechnicianLoadDto>> GetTechnicianLoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var technicians = await _dbContext.Users
            .Where(u => u.Role == Role.Tech)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(cancellationToken);

        var loadLookup = await _dbContext.Requests
            .Where(r => r.AssignedToId != null && r.CreatedAt >= from && r.CreatedAt <= to && r.ClosedAt == null)
            .GroupBy(r => r.AssignedToId!.Value)
            .Select(g => new { TechnicianId = g.Key, Active = g.Count() })
            .ToDictionaryAsync(x => x.TechnicianId, x => x.Active, cancellationToken);

        return technicians
            .Select(t => new TechnicianLoadDto(t.Id, t.DisplayName, loadLookup.TryGetValue(t.Id, out var count) ? count : 0))
            .ToList();
    }
}
