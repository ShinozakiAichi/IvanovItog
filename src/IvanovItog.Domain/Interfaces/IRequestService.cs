using IvanovItog.Domain.Common;
using IvanovItog.Domain.Dtos;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;

namespace IvanovItog.Domain.Interfaces;

public interface IRequestService
{
    Task<IReadOnlyCollection<Request>> GetAsync(RequestFilter filter, CancellationToken cancellationToken = default);
    Task<Result<Request>> CreateAsync(Request request, CancellationToken cancellationToken = default);
    Task<Result<Request>> UpdateAsync(Request request, CancellationToken cancellationToken = default);
    Task<Result<bool>> DeleteAsync(int requestId, CancellationToken cancellationToken = default);
    Task<Result<Request>> AssignAsync(int requestId, int technicianId, CancellationToken cancellationToken = default);
    Task<Result<Request>> CloseAsync(int requestId, CancellationToken cancellationToken = default);
}

public record RequestFilter(
    int? CategoryId,
    int? StatusId,
    Priority? Priority,
    DateTime? CreatedFrom,
    DateTime? CreatedTo,
    string? Search,
    int? CreatedById,
    int? AssignedToId,
    bool IncludeUnassigned
);
