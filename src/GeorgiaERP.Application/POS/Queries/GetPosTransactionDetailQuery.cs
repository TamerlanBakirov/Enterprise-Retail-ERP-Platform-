using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Queries;

public record GetPosTransactionDetailQuery(Guid TransactionId) : IRequest<Result<PosTransactionDetail>>;

public record PosTransactionDetail(
    Guid Id,
    string TransactionNumber,
    string TransactionType,
    Guid StoreId,
    Guid? CustomerId,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal VatTotal,
    decimal Total,
    string Status,
    string? FiscalReceiptId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? VoidedAt,
    string? VoidReason,
    List<PosTransactionLineDetail> Lines,
    List<PosPaymentDetail> Payments);

public record PosTransactionLineDetail(
    int LineNumber,
    Guid ProductId,
    string ProductName,
    string? Barcode,
    decimal Quantity,
    decimal UnitPrice,
    decimal DiscountAmount,
    decimal VatAmount,
    decimal LineTotal);

public record PosPaymentDetail(
    string PaymentMethod,
    decimal Amount,
    string Currency,
    string? Reference,
    decimal? ChangeAmount);

public class GetPosTransactionDetailQueryHandler
    : IRequestHandler<GetPosTransactionDetailQuery, Result<PosTransactionDetail>>
{
    private readonly IAppDbContext _dbContext;

    public GetPosTransactionDetailQueryHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<PosTransactionDetail>> Handle(
        GetPosTransactionDetailQuery request, CancellationToken cancellationToken)
    {
        var tx = await _dbContext.PosTransactions
            .AsNoTracking()
            .Include(t => t.Lines)
            .Include(t => t.Payments)
            .FirstOrDefaultAsync(t => t.Id == request.TransactionId, cancellationToken);

        if (tx is null)
            return Result.Failure<PosTransactionDetail>("Transaction not found.");

        var detail = new PosTransactionDetail(
            tx.Id,
            tx.TransactionNumber,
            tx.TransactionType.ToString(),
            tx.StoreId,
            tx.CustomerId,
            tx.Subtotal,
            tx.DiscountTotal,
            tx.VatTotal,
            tx.Total,
            tx.Status.ToString(),
            tx.FiscalReceiptId,
            tx.CreatedAt,
            tx.VoidedAt,
            tx.VoidReason,
            tx.Lines.OrderBy(l => l.LineNumber).Select(l => new PosTransactionLineDetail(
                l.LineNumber,
                l.ProductId,
                l.ProductName,
                l.Barcode,
                l.Quantity,
                l.UnitPrice,
                l.DiscountAmount,
                l.VatAmount,
                l.LineTotal)).ToList(),
            tx.Payments.Select(p => new PosPaymentDetail(
                p.PaymentMethod.ToString(),
                p.Amount,
                p.Currency,
                p.Reference,
                p.ChangeAmount)).ToList());

        return Result.Success(detail);
    }
}
