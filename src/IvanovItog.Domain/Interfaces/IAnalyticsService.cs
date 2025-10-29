using IvanovItog.Domain.Dtos;

namespace IvanovItog.Domain.Interfaces;

public interface IAnalyticsService
{
    Task<RequestsByStatusDto> GetRequestsByStatusAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<RequestsTimelinePointDto>> GetRequestsTimelineAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<IReadOnlyCollection<TechnicianLoadDto>> GetTechnicianLoadAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}
