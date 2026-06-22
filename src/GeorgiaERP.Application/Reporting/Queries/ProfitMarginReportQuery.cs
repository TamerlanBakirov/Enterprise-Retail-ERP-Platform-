using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.POS;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reporting.Queries;

/// <summary>
/// Generates a profit margin report for a given date range.
/// Analyzes revenue, cost, and profit at the product level from completed POS transactions.
/// </summary>
public record ProfitMarginReportQuery(
    DateTimeOffset From,
    DateTimeOffset To,
    Guid? StoreId = null) : IRequest<ProfitMarginReport>, ICacheable
{
    public string CacheKey => $"reports:profit-margin:{From:yyyyMMdd}:{To:yyyyMMdd}:{StoreId}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(5);
}

public record ProfitMarginReport(
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalProfit,
    decimal OverallMarginPercent,
    int TotalItemsSold,
    DateTimeOffset From,
    DateTimeOffset To,
    List<ProductProfitItem> Products,
    List<CategoryProfitItem> Categories,
    List<DailyProfitItem> DailyBreakdown);

public record ProductProfitItem(
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public record CategoryProfitItem(
    Guid? CategoryId,
    string CategoryName,
    int ProductCount,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public record DailyProfitItem(
    DateOnly Date,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public class ProfitMarginReportQueryHandler : IRequestHandler<ProfitMarginReportQuery, ProfitMarginReport>
{
    private readonly IAppDbContext _dbContext;
    public ProfitMarginReportQueryHandler(IAppDbContext dbContext) => _dbContext = dbContext;

    public async Task<ProfitMarginReport> Handle(ProfitMarginReportQuery request, CancellationToken ct)
    {
        // Get completed transactions in date range
        var txQuery = _dbContext.PosTransactions.AsNoTracking()
            .Where(t => t.Status == PosTransactionStatus.Completed
                        && t.CreatedAt >= request.From
                        && t.CreatedAt <= request.To);

        if (request.StoreId.HasValue)
            txQuery = txQuery.Where(t => t.StoreId == request.StoreId.Value);

        var txIds = await txQuery.Select(t => t.Id).ToListAsync(ct);

        if (txIds.Count == 0)
        {
            return new ProfitMarginReport(0, 0, 0, 0, 0, request.From, request.To,
                new List<ProductProfitItem>(), new List<CategoryProfitItem>(), new List<DailyProfitItem>());
        }

        // Get transaction lines with cost/revenue data
        var lines = await _dbContext.PosTransactionLines.AsNoTracking()
            .Where(l => txIds.Contains(l.TransactionId))
            .ToListAsync(ct);

        // Get product-to-category mapping
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _dbContext.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Name, p.CategoryId })
            .ToListAsync(ct);

        var categoryIds = products.Select(p => p.CategoryId).Distinct().ToList();
        var categories = await _dbContext.Categories.AsNoTracking()
            .Where(c => categoryIds.Contains(c.Id))
            .Select(c => new { c.Id, c.Name })
            .ToListAsync(ct);

        var productMap = products.ToDictionary(p => p.Id);
        var categoryMap = categories.ToDictionary(c => c.Id, c => c.Name);

        // Get transaction dates for daily breakdown
        var txDates = await txQuery.Select(t => new { t.Id, t.CreatedAt }).ToListAsync(ct);
        var txDateMap = txDates.ToDictionary(t => t.Id, t => t.CreatedAt);

        // Product-level profitability
        var productProfit = lines
            .GroupBy(l => l.ProductId)
            .Select(g =>
            {
                var revenue = g.Sum(l => l.LineTotal);
                var cost = g.Sum(l => l.CostPrice * l.Quantity);
                var profit = revenue - cost;
                var productName = productMap.TryGetValue(g.Key, out var p) ? p.Name : "Unknown";

                return new ProductProfitItem(
                    g.Key,
                    productName,
                    (int)g.Sum(l => l.Quantity),
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0);
            })
            .OrderByDescending(p => p.Profit)
            .ToList();

        // Category-level profitability
        var categoryProfit = lines
            .GroupBy(l =>
            {
                if (productMap.TryGetValue(l.ProductId, out var p))
                    return p.CategoryId;
                return Guid.Empty;
            })
            .Select(g =>
            {
                var revenue = g.Sum(l => l.LineTotal);
                var cost = g.Sum(l => l.CostPrice * l.Quantity);
                var profit = revenue - cost;
                var catName = g.Key != Guid.Empty && categoryMap.TryGetValue(g.Key, out var name)
                    ? name : "Uncategorized";

                return new CategoryProfitItem(
                    g.Key != Guid.Empty ? g.Key : null,
                    catName,
                    g.Select(l => l.ProductId).Distinct().Count(),
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0);
            })
            .OrderByDescending(c => c.Profit)
            .ToList();

        // Daily breakdown
        // Build line-to-date mapping via transaction
        var lineWithDate = lines
            .Select(l =>
            {
                var date = txDateMap.TryGetValue(l.TransactionId, out var d) ? d : DateTimeOffset.MinValue;
                return new { Line = l, Date = DateOnly.FromDateTime(date.Date) };
            })
            .ToList();

        var dailyBreakdown = lineWithDate
            .GroupBy(x => x.Date)
            .Select(g =>
            {
                var revenue = g.Sum(x => x.Line.LineTotal);
                var cost = g.Sum(x => x.Line.CostPrice * x.Line.Quantity);
                var profit = revenue - cost;

                return new DailyProfitItem(
                    g.Key,
                    Math.Round(revenue, 2),
                    Math.Round(cost, 2),
                    Math.Round(profit, 2),
                    revenue > 0 ? Math.Round(profit / revenue * 100, 2) : 0);
            })
            .OrderBy(d => d.Date)
            .ToList();

        var totalRevenue = productProfit.Sum(p => p.Revenue);
        var totalCost = productProfit.Sum(p => p.Cost);
        var totalProfit = totalRevenue - totalCost;

        return new ProfitMarginReport(
            totalRevenue,
            totalCost,
            totalProfit,
            totalRevenue > 0 ? Math.Round(totalProfit / totalRevenue * 100, 2) : 0,
            productProfit.Sum(p => p.QuantitySold),
            request.From,
            request.To,
            productProfit,
            categoryProfit,
            dailyBreakdown);
    }
}
