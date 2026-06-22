using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.POS.Commands;

public record CreateDailyClosingCommand(
    Guid StoreId,
    DateTimeOffset ClosingDate,
    Guid ClosedBy) : IRequest<Result<DailyClosingResponse>>;

public record DailyClosingResponse(
    Guid Id,
    decimal TotalSales,
    decimal TotalReturns,
    decimal TotalVat,
    decimal CashTotal,
    decimal CardTotal,
    int TransactionCount,
    string Status);

public class CreateDailyClosingCommandHandler
    : IRequestHandler<CreateDailyClosingCommand, Result<DailyClosingResponse>>
{
    private readonly IAppDbContext _db;
    public CreateDailyClosingCommandHandler(IAppDbContext db) => _db = db;

    public async Task<Result<DailyClosingResponse>> Handle(
        CreateDailyClosingCommand request, CancellationToken ct)
    {
        var storeExists = await _db.Stores.AnyAsync(s => s.Id == request.StoreId && s.IsActive, ct);
        if (!storeExists) return Result.Failure<DailyClosingResponse>("Store not found.");

        var closingDate = request.ClosingDate.Date;
        var alreadyClosed = await _db.DailyClosings
            .AnyAsync(d => d.StoreId == request.StoreId
                && d.ClosingDate.Date == closingDate, ct);

        if (alreadyClosed)
            return Result.Failure<DailyClosingResponse>($"Daily closing already exists for {closingDate:yyyy-MM-dd}.");

        var dayStart = new DateTimeOffset(closingDate, TimeSpan.Zero);
        var dayEnd = dayStart.AddDays(1);

        var transactions = await _db.PosTransactions
            .Where(t => t.StoreId == request.StoreId
                && t.CreatedAt >= dayStart
                && t.CreatedAt < dayEnd
                && t.Status == PosTransactionStatus.Completed)
            .ToListAsync(ct);

        var payments = await _db.PosPayments
            .Where(p => _db.PosTransactions
                .Where(t => t.StoreId == request.StoreId
                    && t.CreatedAt >= dayStart
                    && t.CreatedAt < dayEnd
                    && t.Status == PosTransactionStatus.Completed)
                .Select(t => t.Id)
                .Contains(p.TransactionId))
            .ToListAsync(ct);

        var totalSales = transactions.Where(t => t.TransactionType == PosTransactionType.Sale).Sum(t => t.Total);
        var totalReturns = transactions.Where(t => t.TransactionType == PosTransactionType.Return).Sum(t => t.Total);
        var totalVat = transactions.Sum(t => t.VatTotal);
        var cashTotal = payments.Where(p => p.PaymentMethod == PaymentMethod.Cash).Sum(p => p.Amount);
        var cardTotal = payments.Where(p => p.PaymentMethod == PaymentMethod.Card).Sum(p => p.Amount);
        var otherTotal = payments.Where(p => p.PaymentMethod != PaymentMethod.Cash && p.PaymentMethod != PaymentMethod.Card).Sum(p => p.Amount);

        var closing = DailyClosing.Create(request.StoreId, request.ClosingDate);

        _db.DailyClosings.Add(closing);
        await _db.SaveChangesAsync(ct);

        return Result.Success(new DailyClosingResponse(
            closing.Id, totalSales, totalReturns, totalVat,
            cashTotal, cardTotal, transactions.Count, closing.Status.ToString()));
    }
}
