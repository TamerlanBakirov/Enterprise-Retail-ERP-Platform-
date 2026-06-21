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
        var now = DateTimeOffset.UtcNow;
        var todayStart = new DateTimeOffset(now.Year, now.Month, now.Day, 0, 0, 0, TimeSpan.Zero);
        var todayEnd = todayStart.AddDays(1);

        // Sales KPIs - today's completed transactions.
        // Materialize first to avoid SQLite translation issues with DateTimeOffset + enum filters.
        var todayTxs = await _dbContext.PosTransactions.AsNoTracking()
            .Where(t => t.Status == PosTransactionStatus.Completed)
            .ToListAsync(ct);

        var todayCompleted = todayTxs
            .Where(t => t.CreatedAt >= todayStart && t.CreatedAt < todayEnd)
            .ToList();

        var totalSalesToday = todayCompleted.Sum(t => t.Total);
        var transactionsToday = todayCompleted.Count;

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

        // Compliance KPIs - waybill statuses.
        // Materialize to avoid SQLite enum translation issues.
        var fiscalDocs = await _dbContext.FiscalDocuments.AsNoTracking().ToListAsync(ct);
        var pendingWaybills = fiscalDocs.Count(d => d.Status == FiscalDocumentStatus.Queued || d.Status == FiscalDocumentStatus.Pending);
        var failedWaybills = fiscalDocs.Count(d => d.Status == FiscalDocumentStatus.Failed);
        var submittedToday = fiscalDocs.Count(d => d.Status == FiscalDocumentStatus.Submitted
                          && d.SubmittedAt.HasValue
                          && d.SubmittedAt.Value >= todayStart
                          && d.SubmittedAt.Value < todayEnd);

        // Finance KPIs
        var journalEntries = await _dbContext.JournalEntries.AsNoTracking().ToListAsync(ct);
        var draftJournalEntries = journalEntries.Count(j => j.Status == Domain.Finance.JournalEntryStatus.Draft);

        // Receivables/Payables from bank account balances (simplified)
        var bankAccounts = await _dbContext.BankAccounts.AsNoTracking().ToListAsync(ct);
        var totalReceivables = bankAccounts.Where(a => a.CurrentBalance > 0).Sum(a => a.CurrentBalance);
        var totalPayables = bankAccounts.Where(a => a.CurrentBalance < 0).Sum(a => Math.Abs(a.CurrentBalance));

        // Operational KPIs
        var activePosTerminals = await _dbContext.PosTerminals.AsNoTracking()
            .CountAsync(t => t.IsActive, ct);

        var purchaseOrders = await _dbContext.PurchaseOrders.AsNoTracking().ToListAsync(ct);
        var pendingPurchaseOrders = purchaseOrders.Count(po =>
            po.Status == Domain.Procurement.PurchaseOrderStatus.PendingApproval
            || po.Status == Domain.Procurement.PurchaseOrderStatus.Approved
            || po.Status == Domain.Procurement.PurchaseOrderStatus.Sent);

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
