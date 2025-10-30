using System.Linq;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared.Dtos;
using Microsoft.EntityFrameworkCore;

namespace IvanovItog.Infrastructure.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly AppDbContext _dbContext;
    private const string UnknownStatusName = "Не задан";

    public AnalyticsService(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyCollection<RequestsByStatusDto>> GetRequestsByStatusAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        NormalizeRange(ref from, ref to);

        var statuses = await _dbContext.Requests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.StatusId)
            .Select(group => new
            {
                group.Key,
                Count = group.Count()
            })
            .GroupJoin(
                _dbContext.Statuses.AsNoTracking(),
                group => group.Key,
                status => status.Id,
                (group, statuses) => new { group, statuses })
            .SelectMany(
                x => x.statuses.DefaultIfEmpty(),
                (x, status) => new RequestsByStatusDto
                {
                    Status = status != null ? status.Name : UnknownStatusName,
                    Count = x.group.Count
                })
            .OrderBy(dto => dto.Status)
            .ToListAsync(cancellationToken);

        return statuses;
    }

    public async Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(
    DateTime from,
    DateTime to,
    CancellationToken cancellationToken = default)
    {
        NormalizeRange(ref from, ref to);

        var grouped = await _dbContext.Requests
            .AsNoTracking()
            .Where(r => r.CreatedAt >= from && r.CreatedAt <= to)
            .GroupBy(r => r.CreatedAt.Date)
            .Select(g => new { Date = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Date, x => x.Count, cancellationToken);

        var result = new List<RequestsTimelinePointDto>();
        for (var day = from.Date; day <= to.Date; day = day.AddDays(1))
        {
            result.Add(new RequestsTimelinePointDto
            {
                Date = day,
                Count = grouped.TryGetValue(day, out var count) ? count : 0
            });
        }

        return result;
    }


    public async Task<IReadOnlyCollection<TechnicianLoadDto>> GetTechnicianLoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        NormalizeRange(ref from, ref to);

        var technicians = await _dbContext.Users
            .AsNoTracking()
            .Where(u => u.Role == Role.Tech)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(cancellationToken);

        var activeLookup = await _dbContext.Requests
            .AsNoTracking()
            .Where(r => r.AssignedToId != null && r.CreatedAt >= from && r.CreatedAt <= to && r.ClosedAt == null)
            .GroupBy(r => r.AssignedToId!.Value)
            .Select(g => new { TechnicianId = g.Key, Active = g.Count() })
            .ToDictionaryAsync(x => x.TechnicianId, x => x.Active, cancellationToken);

        var closedLookup = await _dbContext.Requests
            .AsNoTracking()
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

    private static void NormalizeRange(ref DateTime from, ref DateTime to)
    {
        if (from <= to)
        {
            return;
        }

        (from, to) = (to, from);
    }
}
