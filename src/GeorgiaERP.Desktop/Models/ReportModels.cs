namespace GeorgiaERP.Desktop.Models;

public record SalesReportDto(
    decimal TotalRevenue,
    decimal TotalVat,
    int TransactionCount,
    decimal AverageTransactionValue,
    List<DailySalesDto> DailySummary,
    List<PaymentMethodBreakdownDto> PaymentBreakdown,
    List<TopSellingItemDto> TopItems);

public record DailySalesDto(
    DateTimeOffset Date,
    decimal Revenue,
    int TransactionCount);

public record PaymentMethodBreakdownDto(
    string PaymentMethod,
    decimal Amount,
    int Count);

public record TopSellingItemDto(
    string ProductName,
    decimal Quantity,
    decimal Revenue);

public record StockReportDto(
    decimal TotalStockValue,
    int TotalProducts,
    int LowStockCount,
    int OutOfStockCount,
    List<StockLevelDto> StockLevels);

public record VatReportDto(
    int Year,
    int Month,
    decimal TotalSalesVat,
    decimal TotalPurchaseVat,
    decimal NetVat,
    int TotalFiscalDocuments,
    int SubmittedDocuments,
    int PendingDocuments,
    int FailedDocuments);

public record ProfitMarginReportDto(
    decimal TotalRevenue,
    decimal TotalCost,
    decimal TotalProfit,
    decimal OverallMarginPercent,
    int TotalItemsSold,
    DateTimeOffset From,
    DateTimeOffset To,
    List<ProductProfitItemDto> Products,
    List<CategoryProfitItemDto> Categories,
    List<DailyProfitItemDto> DailyBreakdown);

public record ProductProfitItemDto(
    Guid ProductId,
    string ProductName,
    int QuantitySold,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public record CategoryProfitItemDto(
    Guid? CategoryId,
    string CategoryName,
    int ProductCount,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public record DailyProfitItemDto(
    string Date,
    decimal Revenue,
    decimal Cost,
    decimal Profit,
    decimal MarginPercent);

public record TopSellingProductsReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalUniqueProducts,
    decimal TotalRevenue,
    int TotalItemsSold,
    List<TopSellingProductItemDto> Products);

public record TopSellingProductItemDto(
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

public record SupplierPerformanceReportDto(
    DateTimeOffset From,
    DateTimeOffset To,
    int TotalSuppliers,
    decimal TotalSpend,
    int TotalOrders,
    int TotalDeliveries,
    List<SupplierPerformanceItemDto> Suppliers);

public record SupplierPerformanceItemDto(
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
