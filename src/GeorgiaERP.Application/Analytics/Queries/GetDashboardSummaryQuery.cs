using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Analytics.Queries;

public record GetDashboardSummaryQuery : IRequest<DashboardSummaryDto>;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IAppDbContext _dbContext;

    public GetDashboardSummaryQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken ct)
    {
        var completedSales = _dbContext.PosTransactions
            .Where(t => t.Status == PosTransactionStatus.Completed
                     && t.TransactionType == PosTransactionType.Sale);

        var totalRevenue = await completedSales.SumAsync(t => (decimal?)t.Total, ct) ?? 0;
        var totalOrders = await completedSales.CountAsync(ct);
        var averageOrderValue = totalOrders > 0 ? Math.Round(totalRevenue / totalOrders, 2) : 0;

        var totalProducts = await _dbContext.Products.CountAsync(ct);

        var lowStockCount = await _dbContext.StockLevels
            .Join(_dbContext.Products, s => s.ProductId, p => p.Id, (s, p) => new { s, p })
            .CountAsync(x => x.p.MinStockLevel.HasValue && x.s.QuantityOnHand <= x.p.MinStockLevel.Value, ct);

        var activeCustomers = await _dbContext.Customers
            .CountAsync(c => c.IsActive, ct);

        // Today's revenue: load to memory to avoid SQLite DateTimeOffset translation issues
        var allSales = await completedSales
            .Select(t => new { t.CreatedAt, t.Total })
            .ToListAsync(ct);
        var today = DateTimeOffset.UtcNow.Date;
        var todayRevenue = allSales
            .Where(t => t.CreatedAt.Date == today)
            .Sum(t => t.Total);

        // Top selling products: aggregate from transaction lines
        var topProducts = await _dbContext.PosTransactionLines
            .Where(l => completedSales.Select(t => t.Id).Contains(l.TransactionId))
            .GroupBy(l => l.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                QuantitySold = g.Sum(l => l.Quantity),
                Revenue = g.Sum(l => l.LineTotal)
            })
            .ToListAsync(ct);

        // Sort in memory to avoid SQLite DateTimeOffset issues with complex queries
        var topProductsSorted = topProducts
            .OrderByDescending(p => p.Revenue)
            .Take(5)
            .ToList();

        // Fetch product details for top products
        var topProductIds = topProductsSorted.Select(p => p.ProductId).ToList();
        var productDetails = await _dbContext.Products
            .Where(p => topProductIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Sku })
            .ToListAsync(ct);

        var topSellingProducts = topProductsSorted.Select(tp =>
        {
            var details = productDetails.FirstOrDefault(p => p.Id == tp.ProductId);
            return new TopProductDto(
                tp.ProductId,
                details?.Name ?? "Unknown",
                details?.Sku ?? "Unknown",
                tp.QuantitySold,
                tp.Revenue);
        }).ToList();

        // Revenue trend: last 7 days - reuse in-memory allSales
        var sevenDaysAgo = today.AddDays(-6);
        var revenueTrend = allSales
            .Where(t => t.CreatedAt.Date >= sevenDaysAgo)
            .GroupBy(t => t.CreatedAt.Date)
            .Select(g => new RevenueTrendPoint(
                g.Key.ToString("yyyy-MM-dd"),
                g.Sum(t => t.Total),
                g.Count()))
            .OrderBy(r => r.Date)
            .ToList();

        return new DashboardSummaryDto(
            totalRevenue,
            totalOrders,
            averageOrderValue,
            totalProducts,
            lowStockCount,
            activeCustomers,
            todayRevenue,
            topSellingProducts,
            revenueTrend);
    }
}
