using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Procurement;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

/// <summary>
/// Analyzes supplier performance: order volume, delivery reliability, and spend metrics.
/// </summary>
public record SupplierPerformanceQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? SupplierId = null) : IRequest<SupplierPerformanceReport>, ICacheable
{
    public string CacheKey => $"reports:supplier-perf:{From:yyyyMMdd}:{To:yyyyMMdd}:{SupplierId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record SupplierPerformanceReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalSuppliers,
    decimal TotalSpend,
    int TotalOrders,
    int TotalDeliveries,
    List<SupplierPerformanceItem> Suppliers);

public record SupplierPerformanceItem(
    Guid SupplierId,
    string SupplierName,
    string? SupplierCode,
    int? Rating,
    int TotalOrders,
    int CompletedOrders,
    int CancelledOrders,
    int DeliveriesReceived,
    decimal TotalSpend,
    decimal AverageOrderValue,
    int? AverageLeadTimeDays,
    decimal OnTimeDeliveryPercent,
    decimal OrderFulfillmentPercent);

public class SupplierPerformanceQueryHandler : IRequestHandler<SupplierPerformanceQuery, SupplierPerformanceReport>
{
    private readonly IAppDbContext _dbContext;
    public SupplierPerformanceQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<SupplierPerformanceReport> Handle(SupplierPerformanceQuery request, CancellationToken ct)
    {
        // Get purchase orders in the date range
        var poQuery = _dbContext.PurchaseOrders.AsNoTracking()
            .Where(po => po.OrderDate >= request.From.ToUniversalTime() && po.OrderDate <= request.To.ToUniversalTime());

        if (request.SupplierId.HasValue)
            poQuery = poQuery.Where(po => po.SupplierId == request.SupplierId.Value);

        var purchaseOrders = await poQuery.ToListAsync(ct);

        if (purchaseOrders.Count == 0 && !request.SupplierId.HasValue)
        {
            return new SupplierPerformanceReport(
                request.From, request.To, 0, 0, 0, 0, new List<SupplierPerformanceItem>());
        }

        // Get supplier details
        var supplierIds = purchaseOrders.Select(po => po.SupplierId).Distinct().ToList();
        if (request.SupplierId.HasValue && !supplierIds.Contains(request.SupplierId.Value))
            supplierIds.Add(request.SupplierId.Value);

        var suppliers = await _dbContext.Suppliers.AsNoTracking()
            .Where(s => supplierIds.Contains(s.Id))
            .ToListAsync(ct);

        // Get goods receipt notes for delivery analysis
        var poIds = purchaseOrders.Select(po => po.Id).ToList();
        var receipts = await _dbContext.GoodsReceiptNotes.AsNoTracking()
            .Where(grn => poIds.Contains(grn.PurchaseOrderId))
            .ToListAsync(ct);

        var supplierPerformance = suppliers.Select(supplier =>
        {
            var supplierPOs = purchaseOrders.Where(po => po.SupplierId == supplier.Id).ToList();
            var supplierReceipts = receipts
                .Where(r => r.SupplierId == supplier.Id)
                .ToList();

            var completedOrders = supplierPOs.Count(po =>
                po.Status == PurchaseOrderStatus.Received
                || po.Status == PurchaseOrderStatus.PartiallyReceived);

            var cancelledOrders = supplierPOs.Count(po =>
                po.Status == PurchaseOrderStatus.Cancelled);

            var totalSpend = supplierPOs
                .Where(po => po.Status != PurchaseOrderStatus.Cancelled)
                .Sum(po => po.Total);

            // Calculate average lead time (order date to receipt date)
            var leadTimes = new List<int>();
            foreach (var receipt in supplierReceipts)
            {
                var po = supplierPOs.FirstOrDefault(p => p.Id == receipt.PurchaseOrderId);
                if (po is not null)
                {
                    var days = (receipt.ReceiptDate - po.OrderDate).Days;
                    if (days >= 0) leadTimes.Add(days);
                }
            }

            var avgLeadTime = leadTimes.Count > 0 ? (int?)Math.Round(leadTimes.Average()) : null;

            // On-time delivery: orders that have expected date and were received on or before
            var ordersWithExpectedDate = supplierPOs
                .Where(po => po.ExpectedDate.HasValue)
                .ToList();

            decimal onTimePercent = 0;
            if (ordersWithExpectedDate.Count > 0)
            {
                var onTimeCount = ordersWithExpectedDate.Count(po =>
                {
                    var receipt = supplierReceipts.FirstOrDefault(r => r.PurchaseOrderId == po.Id);
                    return receipt is not null && receipt.ReceiptDate <= po.ExpectedDate!.Value;
                });
                onTimePercent = Math.Round((decimal)onTimeCount / ordersWithExpectedDate.Count * 100, 2);
            }

            // Order fulfillment rate
            var fulfillmentPercent = supplierPOs.Count > 0
                ? Math.Round((decimal)completedOrders / supplierPOs.Count * 100, 2)
                : 0m;

            return new SupplierPerformanceItem(
                supplier.Id,
                supplier.Name,
                supplier.Code,
                supplier.Rating,
                supplierPOs.Count,
                completedOrders,
                cancelledOrders,
                supplierReceipts.Count,
                Math.Round(totalSpend, 2),
                supplierPOs.Count > 0 ? Math.Round(totalSpend / supplierPOs.Count, 2) : 0,
                avgLeadTime,
                onTimePercent,
                fulfillmentPercent);
        })
        .OrderByDescending(s => s.TotalSpend)
        .ToList();

        return new SupplierPerformanceReport(
            request.From,
            request.To,
            supplierPerformance.Count,
            supplierPerformance.Sum(s => s.TotalSpend),
            purchaseOrders.Count,
            receipts.Count,
            supplierPerformance);
    }
}
