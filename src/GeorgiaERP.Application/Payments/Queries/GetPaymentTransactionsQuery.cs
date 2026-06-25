using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Payments.Queries;

public record GetPaymentTransactionsQuery(
    Guid? OrderId = null,
    string? Status = null,
    string? Provider = null,
    DateTimeOffset? From = null,
    DateTimeOffset? To = null,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<PaymentTransactionDto>>;

public record GetPaymentTransactionByIdQuery(Guid Id) : IRequest<Result<PaymentTransactionDto>>;

public class GetPaymentTransactionsQueryHandler
    : IRequestHandler<GetPaymentTransactionsQuery, PagedResult<PaymentTransactionDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetPaymentTransactionsQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<PagedResult<PaymentTransactionDto>> Handle(
        GetPaymentTransactionsQuery request, CancellationToken cancellationToken)
    {
        var query = _dbContext.PaymentTransactions.AsQueryable();

        if (request.OrderId.HasValue)
            query = query.Where(t => t.OrderId == request.OrderId.Value);

        if (!string.IsNullOrEmpty(request.Status) &&
            Enum.TryParse<PaymentStatus>(request.Status, true, out var status))
            query = query.Where(t => t.Status == status);

        if (!string.IsNullOrEmpty(request.Provider) &&
            Enum.TryParse<PaymentProvider>(request.Provider, true, out var provider))
            query = query.Where(t => t.Provider == provider);

        if (request.From.HasValue)
            query = query.Where(t => t.CreatedAt >= request.From.Value);

        if (request.To.HasValue)
            query = query.Where(t => t.CreatedAt <= request.To.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(t => t.CreatedAt)
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new PaymentTransactionDto(
                t.Id, t.OrderId, t.Amount, t.Currency,
                t.Provider.ToString(), t.Status.ToString(),
                t.ExternalTransactionId, t.ErrorMessage,
                t.CreatedAt, t.CompletedAt, t.Metadata))
            .ToListAsync(cancellationToken);

        return new PagedResult<PaymentTransactionDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}

public class GetPaymentTransactionByIdQueryHandler
    : IRequestHandler<GetPaymentTransactionByIdQuery, Result<PaymentTransactionDto>>
{
    private readonly IAppDbContext _dbContext;

    public GetPaymentTransactionByIdQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PaymentTransactionDto>> Handle(
        GetPaymentTransactionByIdQuery request, CancellationToken cancellationToken)
    {
        var transaction = await _dbContext.PaymentTransactions
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (transaction is null)
            return Result.NotFound<PaymentTransactionDto>("PaymentTransaction", request.Id);

        return Result.Success(new PaymentTransactionDto(
            transaction.Id, transaction.OrderId, transaction.Amount, transaction.Currency,
            transaction.Provider.ToString(), transaction.Status.ToString(),
            transaction.ExternalTransactionId, transaction.ErrorMessage,
            transaction.CreatedAt, transaction.CompletedAt, transaction.Metadata));
    }
}
