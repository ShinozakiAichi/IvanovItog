using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Shared.Dtos;

namespace IvanovItog.App;

internal sealed class FakeAnalyticsService : IAnalyticsService
{
    private readonly IReadOnlyCollection<RequestsTimelinePointDto> _timeline;
    private readonly IReadOnlyCollection<RequestsByStatusDto> _statuses;
    private readonly IReadOnlyCollection<TechnicianLoadDto> _loads;

    public FakeAnalyticsService()
    {
        var today = DateTime.Today;

        _timeline = Enumerable.Range(0, 30)
            .Select(offset => new RequestsTimelinePointDto
            {
                Date = today.AddDays(-offset),
                Count = 12 + (offset % 6) * 3
            })
            .OrderBy(point => point.Date)
            .ToArray();

        _statuses = new RequestsByStatusDto[]
        {
            new() { Status = "Открыта", Count = 24 },
            new() { Status = "В работе", Count = 18 },
            new() { Status = "Завершена", Count = 31 },
            new() { Status = "Отменена", Count = 6 }
        };

        _loads = new TechnicianLoadDto[]
        {
            new() { TechnicianName = "Иванов И.И.", ActiveRequests = 5, ClosedRequests = 14 },
            new() { TechnicianName = "Петров П.П.", ActiveRequests = 7, ClosedRequests = 11 },
            new() { TechnicianName = "Сидорова А.А.", ActiveRequests = 3, ClosedRequests = 9 },
            new() { TechnicianName = "Кузнецов Д.Д.", ActiveRequests = 6, ClosedRequests = 7 }
        };
    }

    public Task<IReadOnlyCollection<RequestsByStatusDto>> GetRequestsByStatusAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_statuses);
    }

    public Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var fromDate = from.Date;
        var toDate = to.Date;

        var filtered = _timeline
            .Where(point => point.Date.Date >= fromDate && point.Date.Date <= toDate)
            .OrderBy(point => point.Date)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<RequestsTimelinePointDto>>(filtered);
    }

    public Task<IReadOnlyCollection<TechnicianLoadDto>> GetTechnicianLoadAsync(
        DateTime from,
        DateTime to,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(_loads);
    }
}
