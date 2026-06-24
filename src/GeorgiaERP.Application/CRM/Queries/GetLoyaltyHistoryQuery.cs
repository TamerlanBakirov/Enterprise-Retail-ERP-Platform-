using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.CRM.Queries;

public record LoyaltyTransactionDto(
    Guid Id,
    Guid CustomerId,
    string TransactionType,
    int Points,
    int BalanceAfter,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Description,
    DateTimeOffset CreatedAt);

/// <summary>
/// Returns a customer's loyalty-point ledger, newest first. The earn/redeem
/// commands write these rows; this is the read side that lets staff and the
/// customer view how a balance was reached.
/// </summary>
public record GetLoyaltyHistoryQuery(
    Guid CustomerId,
    int Page = 1,
    int PageSize = 20) : IRequest<Result<PagedResult<LoyaltyTransactionDto>>>;

public class GetLoyaltyHistoryQueryHandler
    : IRequestHandler<GetLoyaltyHistoryQuery, Result<PagedResult<LoyaltyTransactionDto>>>
{
    private readonly IAppDbContext _dbContext;

    public GetLoyaltyHistoryQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<Result<PagedResult<LoyaltyTransactionDto>>> Handle(GetLoyaltyHistoryQuery request, CancellationToken ct)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        // Distinguish "customer has no transactions" from "customer does not exist",
        // matching the 404 behaviour of the earn/redeem endpoints.
        var customerExists = await _dbContext.Customers.AsNoTracking()
            .AnyAsync(c => c.Id == request.CustomerId, ct);
        if (!customerExists)
            return Result.NotFound<PagedResult<LoyaltyTransactionDto>>("Customer", request.CustomerId);

        var query = _dbContext.LoyaltyTransactions.AsNoTracking()
            .Where(t => t.CustomerId == request.CustomerId);

        var totalCount = await query.CountAsync(ct);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new LoyaltyTransactionDto(
                t.Id,
                t.CustomerId,
                t.TransactionType.ToString(),
                t.Points,
                t.BalanceAfter,
                t.ReferenceType,
                t.ReferenceId,
                t.Description,
                t.CreatedAt))
            .ToListAsync(ct);

        return Result.Success(new PagedResult<LoyaltyTransactionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
}
