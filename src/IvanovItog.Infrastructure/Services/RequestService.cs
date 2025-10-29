using IvanovItog.Domain.Common;
using IvanovItog.Domain.Entities;
using IvanovItog.Domain.Enums;
using IvanovItog.Domain.Interfaces;
using IvanovItog.Infrastructure.Validation;
using Microsoft.EntityFrameworkCore;
using Serilog;

namespace IvanovItog.Infrastructure.Services;

public class RequestService : IRequestService
{
    private readonly AppDbContext _dbContext;
    private readonly RequestValidator _validator;
    private readonly ILogger _logger;

    public RequestService(AppDbContext dbContext, RequestValidator validator, ILogger logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger.ForContext<RequestService>();
    }

    public async Task<Result<Request>> CreateAsync(Request request, CancellationToken cancellationToken = default)
    {
        request.CreatedAt = DateTime.UtcNow;
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.Warning("Request creation failed: {Errors}", errors);
            return Result<Request>.Failure(errors);
        }

        await _dbContext.Requests.AddAsync(request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Created request {RequestId} with title {Title}", request.Id, request.Title);
        return Result<Request>.Success(request);
    }

    public async Task<Result<bool>> DeleteAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.Requests.FindAsync(new object?[] { requestId }, cancellationToken);
        if (request is null)
        {
            return Result<bool>.Failure("RequestNotFound");
        }

        _dbContext.Requests.Remove(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Deleted request {RequestId}", requestId);
        return Result<bool>.Success(true);
    }

    public async Task<IReadOnlyCollection<Request>> GetAsync(RequestFilter filter, CancellationToken cancellationToken = default)
    {
        IQueryable<Request> query = _dbContext.Requests
            .Include(r => r.Category)
            .Include(r => r.Status)
            .Include(r => r.CreatedBy)
            .Include(r => r.AssignedTo)
            .AsNoTracking();

        if (filter.CategoryId.HasValue)
        {
            query = query.Where(r => r.CategoryId == filter.CategoryId);
        }

        if (filter.StatusId.HasValue)
        {
            query = query.Where(r => r.StatusId == filter.StatusId);
        }

        if (filter.Priority.HasValue)
        {
            query = query.Where(r => r.Priority == filter.Priority);
        }

        if (filter.CreatedFrom.HasValue)
        {
            query = query.Where(r => r.CreatedAt >= filter.CreatedFrom);
        }

        if (filter.CreatedTo.HasValue)
        {
            query = query.Where(r => r.CreatedAt <= filter.CreatedTo);
        }

        if (!string.IsNullOrWhiteSpace(filter.Search))
        {
            var pattern = $"%{filter.Search.Trim()}%";
            query = query.Where(r => EF.Functions.Like(r.Title, pattern) || EF.Functions.Like(r.Description, pattern));
        }

        if (filter.CreatedById.HasValue)
        {
            query = query.Where(r => r.CreatedById == filter.CreatedById.Value);
        }

        if (filter.AssignedToId.HasValue)
        {
            var technicianId = filter.AssignedToId.Value;
            if (filter.IncludeUnassigned)
            {
                query = query.Where(r => r.AssignedToId == technicianId || r.AssignedToId == null);
            }
            else
            {
                query = query.Where(r => r.AssignedToId == technicianId);
            }
        }
        else if (filter.IncludeUnassigned)
        {
            query = query.Where(r => r.AssignedToId == null);
        }

        var requests = await query
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        return requests;
    }

    public async Task<Result<Request>> UpdateAsync(Request request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = string.Join(";", validationResult.Errors.Select(e => e.ErrorMessage));
            _logger.Warning("Request update failed for {RequestId}: {Errors}", request.Id, errors);
            return Result<Request>.Failure(errors);
        }

        _dbContext.Requests.Update(request);
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Updated request {RequestId}", request.Id);
        return Result<Request>.Success(request);
    }

    public async Task<Result<Request>> AssignAsync(int requestId, int technicianId, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.Requests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null)
        {
            return Result<Request>.Failure("RequestNotFound");
        }

        var technicianExists = await _dbContext.Users.AnyAsync(u => u.Id == technicianId && u.Role == Role.Tech, cancellationToken);
        if (!technicianExists)
        {
            return Result<Request>.Failure("TechnicianNotFound");
        }

        request.AssignedToId = technicianId;
        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Assigned request {RequestId} to technician {TechnicianId}", request.Id, technicianId);
        return Result<Request>.Success(request);
    }

    public async Task<Result<Request>> CloseAsync(int requestId, CancellationToken cancellationToken = default)
    {
        var request = await _dbContext.Requests.SingleOrDefaultAsync(r => r.Id == requestId, cancellationToken);
        if (request is null)
        {
            return Result<Request>.Failure("RequestNotFound");
        }

        request.ClosedAt = DateTime.UtcNow;
        if (!await _dbContext.Statuses.AnyAsync(s => s.Id == request.StatusId && s.Name == "Закрыта", cancellationToken))
        {
            var closedStatus = await _dbContext.Statuses.FirstOrDefaultAsync(s => s.Name == "Закрыта", cancellationToken);
            if (closedStatus is not null)
            {
                request.StatusId = closedStatus.Id;
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        _logger.Information("Closed request {RequestId}", request.Id);
        return Result<Request>.Success(request);
    }
}
