using IvanovItog.Shared.Dtos;

namespace IvanovItog.Domain.Interfaces;

public interface IAnalyticsService
{
    Task<IReadOnlyCollection<RequestsByStatusDto>> GetRequestsByStatusAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TechnicianLoadDto>> GetTechnicianLoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
