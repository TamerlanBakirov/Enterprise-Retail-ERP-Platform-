using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

/// <summary>
/// Returns quick KPI summary for the desktop dashboard.
/// Cached for 2 minutes since it aggregates across several tables.
/// </summary>
public record DashboardKpiQuery : IRequest<DashboardKpiResult>, ICacheable
{
    public string CacheKey => "dashboard:kpi";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(2);
}

public record DashboardKpiResult(
    // Sales KPIs (today)
    decimal TotalSalesToday,
    int TransactionsToday,
    decimal AverageTransactionValue,

    // Inventory KPIs
    int TotalProducts,
    int LowStockItemsCount,
    int OutOfStockItemsCount,
    decimal TotalStockValue,

    // Compliance KPIs
    int PendingWaybills,
    int FailedWaybills,
    int SubmittedWaybillsToday,

    // Finance KPIs
    int DraftJournalEntries,
    decimal TotalReceivables,
    decimal TotalPayables,

    // Operational KPIs
    int ActivePosTerminals,
    int PendingPurchaseOrders,
    DateTimeOffset GeneratedAt);

public class DashboardKpiQueryHandler : IRequestHandler<DashboardKpiQuery, DashboardKpiResult>
{
    private readonly IAppDbContext _dbContext;

    public DashboardKpiQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<DashboardKpiResult> Handle(DashboardKpiQuery request, CancellationToken ct)
    {
        var todayStart = DateTimeOffset.UtcNow.Date;
        var todayEnd = todayStart.AddDays(1);

        // Sales KPIs - today's completed transactions
        var todayTxQuery = _dbContext.PosTransactions.AsNoTracking()
            .Where(t => t.CreatedAt >= todayStart && t.CreatedAt < todayEnd
                        && t.Status == PosTransactionStatus.Completed);

        var totalSalesToday = await todayTxQuery.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var transactionsToday = await todayTxQuery.CountAsync(ct);

        // Inventory KPIs
        var stockQuery = _dbContext.StockLevels.AsNoTracking();
        var stockWithProducts = from s in stockQuery
                                join p in _dbContext.Products.AsNoTracking() on s.ProductId equals p.Id
                                select new { s.QuantityOnHand, s.CostPrice, p.MinStockLevel };

        var stockData = await stockWithProducts.ToListAsync(ct);
        var totalProducts = await _dbContext.Products.AsNoTracking().CountAsync(ct);
        var lowStockCount = stockData.Count(s => s.MinStockLevel.HasValue && s.QuantityOnHand <= s.MinStockLevel.Value && s.QuantityOnHand > 0);
        var outOfStockCount = stockData.Count(s => s.QuantityOnHand <= 0);
        var totalStockValue = stockData.Sum(s => s.QuantityOnHand * s.CostPrice);

        // Compliance KPIs - waybill statuses
        var pendingWaybills = await _dbContext.FiscalDocuments.AsNoTracking()
            .CountAsync(d => d.Status == FiscalDocumentStatus.Queued || d.Status == FiscalDocumentStatus.Pending, ct);
        var failedWaybills = await _dbContext.FiscalDocuments.AsNoTracking()
            .CountAsync(d => d.Status == FiscalDocumentStatus.Failed, ct);
        var submittedToday = await _dbContext.FiscalDocuments.AsNoTracking()
            .CountAsync(d => d.Status == FiscalDocumentStatus.Submitted
                          && d.SubmittedAt.HasValue
                          && d.SubmittedAt.Value >= todayStart
                          && d.SubmittedAt.Value < todayEnd, ct);

        // Finance KPIs
        var draftJournalEntries = await _dbContext.JournalEntries.AsNoTracking()
            .CountAsync(j => j.Status == Domain.Finance.JournalEntryStatus.Draft, ct);

        // Receivables/Payables from bank account balances (simplified)
        var bankAccounts = await _dbContext.BankAccounts.AsNoTracking().ToListAsync(ct);
        var totalReceivables = bankAccounts.Where(a => a.CurrentBalance > 0).Sum(a => a.CurrentBalance);
        var totalPayables = bankAccounts.Where(a => a.CurrentBalance < 0).Sum(a => Math.Abs(a.CurrentBalance));

        // Operational KPIs
        var activePosTerminals = await _dbContext.PosTerminals.AsNoTracking()
            .CountAsync(t => t.IsActive, ct);

        var pendingPurchaseOrders = await _dbContext.PurchaseOrders.AsNoTracking()
            .CountAsync(po => po.Status == Domain.Procurement.PurchaseOrderStatus.PendingApproval
                           || po.Status == Domain.Procurement.PurchaseOrderStatus.Approved
                           || po.Status == Domain.Procurement.PurchaseOrderStatus.Sent, ct);

        return new DashboardKpiResult(
            TotalSalesToday: totalSalesToday,
            TransactionsToday: transactionsToday,
            AverageTransactionValue: transactionsToday > 0 ? Math.Round(totalSalesToday / transactionsToday, 2) : 0,
            TotalProducts: totalProducts,
            LowStockItemsCount: lowStockCount,
            OutOfStockItemsCount: outOfStockCount,
            TotalStockValue: Math.Round(totalStockValue, 2),
            PendingWaybills: pendingWaybills,
            FailedWaybills: failedWaybills,
            SubmittedWaybillsToday: submittedToday,
            DraftJournalEntries: draftJournalEntries,
            TotalReceivables: totalReceivables,
            TotalPayables: totalPayables,
            ActivePosTerminals: activePosTerminals,
            PendingPurchaseOrders: pendingPurchaseOrders,
            GeneratedAt: DateTimeOffset.UtcNow);
    }
}
