using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

/// <summary>
/// Returns the top-selling products by quantity or revenue for a given period.
/// </summary>
public record TopSellingProductsQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? StoreId = null,
    int Top = 20,
    TopSellingSort SortBy = TopSellingSort.Revenue) : IRequest<TopSellingProductsReport>, ICacheable
{
    public string CacheKey => $"reports:top-selling:{From:yyyyMMdd}:{To:yyyyMMdd}:{StoreId}:{Top}:{SortBy}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public enum TopSellingSort
{
    Revenue,
    Quantity,
    Profit
}

public record TopSellingProductsReport(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalUniqueProducts,
    decimal TotalRevenue,
    int TotalItemsSold,
    List<TopSellingProductItem> Products);

public record TopSellingProductItem(
    int Rank,
    Guid ProductId,
    string ProductName,
    string? Sku,
    string? CategoryName,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent,
    decimal RevenueSharePercent);

public class TopSellingProductsQueryHandler : IRequestHandler<TopSellingProductsQuery, TopSellingProductsReport>
{
    private readonly IAppDbContext _dbContext;
    public TopSellingProductsQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<TopSellingProductsReport> Handle(TopSellingProductsQuery request, CancellationToken ct)
    {
        var txQuery = _dbContext.PosTransactions.AsNoTracking()
            .Where(t => t.Status == PosTransactionStatus.Completed
                        && t.CreatedAt >= request.From.ToUniversalTime()
                        && t.CreatedAt <= request.To.ToUniversalTime());

        if (request.StoreId.HasValue)
            txQuery = txQuery.Where(t => t.StoreId == request.StoreId.Value);

        var txIds = await txQuery.Select(t => t.Id).ToListAsync(ct);

        if (txIds.Count == 0)
        {
            return new TopSellingProductsReport(request.From, request.To, 0, 0, 0, new List<TopSellingProductItem>());
        }

        var lines = await _dbContext.PosTransactionLines.AsNoTracking()
            .Where(l => txIds.Contains(l.TransactionId))
            .ToListAsync(ct);

        // Get product details
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.Sku, p.CategoryId })
            .ToListAsync(ct);

        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var categories = await _dbContext.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, ct);

        var productMap = products.ToDictionary(p => p.Id);

        var totalRevenue = lines.Sum(l => l.LineTotal);

        var productData = lines
            .GroupBy(l => l.ProductId)
            .Select(g =>
            {
                var revenue = g.Sum(l => l.LineTotal);
                var cost = g.Sum(l => l.CostPrice * l.Quantity);
                var profit = revenue - cost;
                var qty = (int)g.Sum(l => l.Quantity);

                var name = productMap.TryGetValue(g.Key, out var p) ? p.Name : "Unknown";
                var sku = productMap.TryGetValue(g.Key, out var ps) ? ps.Sku : null;
                var catName = "Uncategorized";
                if (productMap.TryGetValue(g.Key, out var pc))
                {
                    categories.TryGetValue(pc.CategoryId, out var cn);
                    catName = cn ?? "Uncategorized";
                }

                return new
                {
                    ProductId = g.Key,
                    Name = name,
                    Sku = sku,
                    CategoryName = catName,
                    QuantitySold = qty,
                    Revenue = Math.Round(revenue, 2),
                    Cost = Math.Round(cost, 2),
                    Profit = Math.Round(profit, 2),
                    MarginPercent = revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0m,
                    RevenueShare = totalRevenue > 0 ? Math.Round(revenue / totalRevenue * 100, 2) : 0m
                };
            });

        var sorted = request.SortBy switch
        {
            TopSellingSort.Quantity => productData.OrderByDescending(p => p.QuantitySold),
            TopSellingSort.Profit => productData.OrderByDescending(p => p.Profit),
            _ => productData.OrderByDescending(p => p.Revenue)
        };

        var topProducts = sorted
            .Take(request.Top)
            .Select((p, i) => new TopSellingProductItem(
                i + 1, p.ProductId, p.Name, p.Sku, p.CategoryName,
                p.QuantitySold, p.Revenue, p.Cost, p.Profit,
                p.MarginPercent, p.RevenueShare))
            .ToList();

        return new TopSellingProductsReport(
            request.From,
            request.To,
            productIds.Count,
            Math.Round(totalRevenue, 2),
            (int)lines.Sum(l => l.Quantity),
            topProducts);
    }
}
