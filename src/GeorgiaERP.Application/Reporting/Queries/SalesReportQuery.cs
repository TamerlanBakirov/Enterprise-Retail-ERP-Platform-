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
        var txQuery = _dbContext.PosTransactions
            .Where(t => t.CreatedAt >= request.From && t.CreatedAt <= request.To);

        if (request.StoreId.HasValue)
            txQuery = txQuery.Where(t => t.StoreId == request.StoreId.Value);

        var completed = txQuery.Where(t => t.Status == PosTransactionStatus.Completed);
        var voided = txQuery.Where(t => t.Status == PosTransactionStatus.Voided);

        var totalSales = await completed.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var totalReturns = await voided.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var totalVat = await completed.SumAsync(t => (decimal?)t.VatTotal, ct) ?? 0;
        var totalDiscount = await completed.SumAsync(t => (decimal?)t.DiscountTotal, ct) ?? 0;
        var txCount = await completed.CountAsync(ct);

        var itemsSold = await _dbContext.PosTransactionLines
            .Where(l => completed.Select(t => t.Id).Contains(l.TransactionId))
            .SumAsync(l => (int?)l.Quantity, ct) ?? 0;

        var byPayment = await _dbContext.PosPayments
            .Where(p => completed.Select(t => t.Id).Contains(p.TransactionId))
            .GroupBy(p => p.PaymentMethod)
            .Select(g => new SalesByPaymentMethod(g.Key.ToString(), g.Sum(p => p.Amount), g.Count()))
            .ToListAsync(ct);

        var daily = await completed
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new DailySalesSummary(DateOnly.FromDateTime(g.Key), g.Sum(t => t.Total), g.Count()))
            .OrderBy(d => d.Date)
            .ToListAsync(ct);

        return new SalesReport(
            totalSales, totalReturns, totalSales - totalReturns, totalVat, totalDiscount,
            txCount, itemsSold,
            txCount > 0 ? Math.Round(totalSales / txCount, 2) : 0,
            byPayment, daily);
    }
}
