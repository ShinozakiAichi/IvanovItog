using IvanovItog.Shared.Dtos;

namespace IvanovItog.Domain.Interfaces;

public interface IRatingService
{
    Task<IReadOnlyCollection<TechnicianRatingDto>> GetRatingsAsync(CancellationToken cancellationToken = default);
    Task<TechnicianRatingDto?> GetTechnicianRatingAsync(int technicianId, CancellationToken cancellationToken = default);
}
