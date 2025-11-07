using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using LottoTM.Server.Api.Repositories;

namespace LottoTM.Server.Api.Features.Draws.DrawsGetList;

/// <summary>
/// Handler for GetDrawsRequest - retrieves paginated list of draws with sorting
/// </summary>
public class GetDrawsHandler : IRequestHandler<Contracts.GetDrawsRequest, Contracts.GetDrawsResponse>
{
    private readonly AppDbContext _dbContext;
    private readonly IValidator<Contracts.GetDrawsRequest> _validator;
    private readonly ILogger<GetDrawsHandler> _logger;

    public GetDrawsHandler(
        AppDbContext dbContext,
        IValidator<Contracts.GetDrawsRequest> validator,
        ILogger<GetDrawsHandler> logger)
    {
        _dbContext = dbContext;
        _validator = validator;
        _logger = logger;
    }

    public async Task<Contracts.GetDrawsResponse> Handle(
        Contracts.GetDrawsRequest request,
        CancellationToken cancellationToken)
    {
        // 1. Validate request parameters
        var validationResult = await _validator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        try
        {
            // 2. Get total count for pagination metadata
            var totalCount = await _dbContext.Draws.CountAsync(cancellationToken);

            // 3. Calculate offset for pagination
            var offset = (request.Page - 1) * request.PageSize;

            // 4. Build query with eager loading of DrawNumbers
            var drawsQuery = _dbContext.Draws
                .AsNoTracking() // Read-only query for better performance
                .Include(d => d.Numbers) // Eager loading to prevent N+1 problem
                .AsQueryable();

            // 5. Apply dynamic sorting based on request parameters
            drawsQuery = request.SortBy.ToLower() switch
            {
                "drawdate" => request.SortOrder.ToLower() == "asc"
                    ? drawsQuery.OrderBy(d => d.DrawDate)
                    : drawsQuery.OrderByDescending(d => d.DrawDate),
                "createdat" => request.SortOrder.ToLower() == "asc"
                    ? drawsQuery.OrderBy(d => d.CreatedAt)
                    : drawsQuery.OrderByDescending(d => d.CreatedAt),
                _ => drawsQuery.OrderByDescending(d => d.DrawDate) // Fallback to default
            };

            // 6. Apply pagination
            var draws = await drawsQuery
                .Skip(offset)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            // 7. Map entities to DTOs
            var drawDtos = draws.Select(d => new Contracts.DrawDto(
                d.Id,
                d.DrawDate.ToDateTime(TimeOnly.MinValue), // Convert DateOnly to DateTime
                d.Numbers.OrderBy(dn => dn.Position).Select(dn => dn.Number).ToArray(),
                d.CreatedAt
            )).ToList();

            // 8. Calculate total pages
            var totalPages = (int)Math.Ceiling((double)totalCount / request.PageSize);

            // 9. Log success
            _logger.LogInformation(
                "Pobrano {Count} losowań (strona {Page}/{TotalPages})",
                drawDtos.Count, request.Page, totalPages);

            return new Contracts.GetDrawsResponse(
                drawDtos,
                totalCount,
                request.Page,
                request.PageSize,
                totalPages
            );
        }
        catch (ValidationException)
        {
            // Re-throw validation exceptions to be handled by middleware
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas pobierania listy losowań");
            throw;
        }
    }
}
