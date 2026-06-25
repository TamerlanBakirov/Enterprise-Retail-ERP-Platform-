namespace GeorgiaERP.Application.Analytics;

public record DashboardSummaryDto(
    decimal TotalRevenue,
    int TotalOrders,
    decimal AverageOrderValue,
    int TotalProducts,
    int LowStockCount,
    int ActiveCustomers,
    decimal TodayRevenue,
    List<TopProductDto> TopSellingProducts,
    List<RevenueTrendPoint> RevenueTrend);
