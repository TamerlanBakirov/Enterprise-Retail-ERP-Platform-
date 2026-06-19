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
