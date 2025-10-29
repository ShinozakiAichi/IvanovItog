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

    public async Task<IReadOnlyCollection<RequestsByStatusDto>> GetRequestsByStatusAsync(CancellationToken cancellationToken = default)
    {
        var statuses = await _dbContext.Requests
            .Include(r => r.Status)
            .GroupBy(r => r.Status != null ? r.Status.Name : "Не задан")
            .Select(g => new RequestsByStatusDto
            {
                Status = g.Key,
                Count = g.Count()
            })
            .OrderBy(dto => dto.Status)
            .ToListAsync(cancellationToken);

        return statuses;
    }

    public async Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        var timeline = await _dbContext.Requests
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new RequestsTimelinePointDto
            {
                Date = g.Key,
                Count = g.Count()
            })
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

        var activeLookup = await _dbContext.Requests
            .Where(r => r.AssignedToId != null && r.CreatedAt >= from && r.CreatedAt <= to && r.ClosedAt == null)
            .GroupBy(r => r.AssignedToId!.Value)
            .Select(g => new { TechnicianId = g.Key, Active = g.Count() })
            .ToDictionaryAsync(x => x.TechnicianId, x => x.Active, cancellationToken);

        var closedLookup = await _dbContext.Requests
            .Where(r => r.AssignedToId != null && r.CreatedAt >= from && r.CreatedAt <= to && r.ClosedAt != null)
            .GroupBy(r => r.AssignedToId!.Value)
            .Select(g => new { TechnicianId = g.Key, Closed = g.Count() })
            .ToDictionaryAsync(x => x.TechnicianId, x => x.Closed, cancellationToken);

        return technicians
            .Select(t => new TechnicianLoadDto
            {
                TechnicianName = t.DisplayName,
                ActiveRequests = activeLookup.TryGetValue(t.Id, out var active) ? active : 0,
                ClosedRequests = closedLookup.TryGetValue(t.Id, out var closed) ? closed : 0
            })
            .OrderBy(dto => dto.TechnicianName)
            .ToList();
    }
}
