using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

public record SalesReportQuery(
    Guid? StoreId,
    DateTimeOffset From,
    DateTimeOffset To) : IRequest<SalesReport>;

public record SalesReport(
    decimal TotalSales,
    decimal TotalReturns,
    decimal NetSales,
    decimal TotalVat,
    decimal TotalDiscount,
    int TransactionCount,
    int ItemsSold,
    decimal AverageTransactionValue,
    List<SalesByPaymentMethod> ByPaymentMethod,
    List<DailySalesSummary> DailyBreakdown);

public record SalesByPaymentMethod(string Method, decimal Amount, int Count);
public record DailySalesSummary(DateOnly Date, decimal Sales, int Transactions);

public class SalesReportQueryHandler : IRequestHandler<SalesReportQuery, SalesReport>
{
    private readonly IAppDbContext _dbContext;
    public SalesReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<SalesReport> Handle(SalesReportQuery request, CancellationToken ct)
    {
        // Npgsql cannot bind a DateTimeOffset with a non-UTC offset to a
        // timestamptz column; normalise the client-supplied range to UTC.
        var from = request.From.ToUniversalTime();
        var to = request.To.ToUniversalTime();

        var txQuery = _dbContext.PosTransactions.AsNoTracking()
            .Where(t => t.CreatedAt >= from && t.CreatedAt <= to);

        if (request.StoreId.HasValue)
            txQuery = txQuery.Where(t => t.StoreId == request.StoreId.Value);

        var completed = txQuery.Where(t => t.Status == PosTransactionStatus.Completed);
        var voided = txQuery.Where(t => t.Status == PosTransactionStatus.Voided);

        var totalSales = await completed.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var totalReturns = await voided.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var totalVat = await completed.SumAsync(t => (decimal?)t.VatTotal, ct) ?? 0;
        var totalDiscount = await completed.SumAsync(t => (decimal?)t.DiscountTotal, ct) ?? 0;
        var txCount = await completed.CountAsync(ct);

        // Subquery rather than a materialized id list: lines/payments filter via
        // WHERE transaction_id IN (SELECT ...), not a large IN (...) parameter set.
        var completedIds = completed.Select(t => t.Id);

        var itemsSold = await _dbContext.PosTransactionLines.AsNoTracking()
            .Where(l => completedIds.Contains(l.TransactionId))
            .SumAsync(l => (int?)l.Quantity, ct) ?? 0;

        var paymentData = await _dbContext.PosPayments.AsNoTracking()
            .Where(p => completedIds.Contains(p.TransactionId))
            .ToListAsync(ct);

        var byPayment = paymentData
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new SalesByPaymentMethod(g.Key.ToString(), g.Sum(p => p.Amount), g.Count()))
            .ToList();

        // Materialize first, then group client-side to avoid provider-specific
        // DateTimeOffset translation issues (SQLite does not support .Date in SQL).
        var completedTxs = await completed.ToListAsync(ct);

        var daily = completedTxs
            .GroupBy(t => DateOnly.FromDateTime(t.CreatedAt.Date))
            .Select(g => new DailySalesSummary(g.Key, g.Sum(t => t.Total), g.Count()))
            .OrderBy(d => d.Date)
            .ToList();

        return new SalesReport(
            totalSales, totalReturns, totalSales - totalReturns, totalVat, totalDiscount,
            txCount, itemsSold,
            txCount > 0 ? Math.Round(totalSales / txCount, 2) : 0,
            byPayment, daily);
    }
}
