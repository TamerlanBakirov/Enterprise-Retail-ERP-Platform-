using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Queries;

public record GetPosTransactionsQuery(
    Guid? SessionId = null,
    Guid? StoreId = null,
    string? Status = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PosTransactionSummary>>;

public record PosTransactionSummary(
    Guid Id,
    string TransactionNumber,
    string TransactionType,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal VatTotal,
    decimal Total,
    string Status,
    string? FiscalReceiptId,
    int LineCount,
    DateTimeOffset CreatedAt);

public class GetPosTransactionsQueryHandler
    : IRequestHandler<GetPosTransactionsQuery, PagedResult<PosTransactionSummary>>
{
    private readonly IAppDbContext _dbContext;

    public GetPosTransactionsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PosTransactionSummary>> Handle(
        GetPosTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PosTransactions.AsQueryable();

        if (request.SessionId.HasValue)
            query = query.Where(t => t.SessionId == request.SessionId.Value);

        if (request.StoreId.HasValue)
            query = query.Where(t => t.StoreId == request.StoreId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<PosTransactionStatus>(request.Status, true, out var status))
            query = query.Where(t => t.Status == status);

        if (request.From.HasValue)
            query = query.Where(t => t.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.CreatedAt <= request.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new PosTransactionSummary(
                t.Id,
                t.TransactionNumber,
                t.TransactionType.ToString(),
                t.Subtotal,
                t.DiscountTotal,
                t.VatTotal,
                t.Total,
                t.Status.ToString(),
                t.FiscalReceiptId,
                t.Lines.Count,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<PosTransactionSummary>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
