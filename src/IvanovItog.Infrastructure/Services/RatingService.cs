using System.Linq;
using IvanovItog.Domain.Dtos;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IvanovItog.Infrastructure.Services;

public class RatingService : IRatingService
{
    private static readonly TimeSpan ResolutionTarget = TimeSpan.FromDays(3);

    private readonly AppDbContext _dbContext;
    private readonly ILogger _logger;

    public RatingService(AppDbContext dbContext, ILogger logger)
    {
        _dbContext = dbContext;
        _logger = logger.ForContext<RatingService>();
    }

    public async Task<IReadOnlyCollection<TechnicianRatingDto>> GetRatingsAsync(CancellationToken cancellationToken = default)
    {
        var technicians = await _dbContext.Users
            .Where(u => u.Role == Role.Tech)
            .Select(u => new { u.Id, u.DisplayName })
            .ToListAsync(cancellationToken);

        var ratings = new List<TechnicianRatingDto>(technicians.Count);
        foreach (var tech in technicians)
        {
            var closedRequests = await _dbContext.Requests
                .Where(r => r.AssignedToId == tech.Id && r.ClosedAt != null)
                .Select(r => new { r.Priority, r.CreatedAt, r.ClosedAt })
                .ToListAsync(cancellationToken);

            var closedCount = closedRequests.Count();
            var overdueCount = closedRequests.Count(r => r.ClosedAt!.Value - r.CreatedAt > ResolutionTarget);
            var highPriorityCount = closedRequests.Count(r => r.Priority == Priority.High);
            var score = (closedCount * 10) - (overdueCount * 5) + (highPriorityCount * 4);
            ratings.Add(new TechnicianRatingDto(tech.Id, tech.DisplayName, closedCount, overdueCount, highPriorityCount, score));
        }

        return ratings
            .OrderByDescending(r => r.Score)
            .ThenBy(r => r.TechnicianName)
            .ToList();
    }

    public async Task<TechnicianRatingDto?> GetTechnicianRatingAsync(int technicianId, CancellationToken cancellationToken = default)
    {
        var ratings = await GetRatingsAsync(cancellationToken);
        return ratings.SingleOrDefault(r => r.TechnicianId == technicianId);
    }
}
