namespace GeorgiaERP.Application.Analytics;

public record StockSummaryDto(
    int TotalItems,
    int LowStockItems,
    int OutOfStockItems,
    decimal TotalValue);
