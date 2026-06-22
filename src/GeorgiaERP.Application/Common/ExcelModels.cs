namespace GeorgiaERP.Application.Common;

public record ProductExportRow(
    string Sku, string Name, string? NameKa, string CategoryName,
    string UnitOfMeasure, bool VatApplicable, decimal? WeightKg,
    decimal? MinStockLevel, decimal? MaxStockLevel,
    bool IsActive, DateTimeOffset CreatedAt);

public record StockExportRow(
    string Sku, string ProductName, string WarehouseName,
    decimal QuantityOnHand, decimal QuantityReserved, decimal Available,
    decimal CostPrice, decimal StockValue, bool IsLowStock);

public record ProductImportRow(
    int RowNumber, string Sku, string Name, string? NameKa,
    string CategoryCode, string UnitOfMeasure, bool VatApplicable,
    decimal? WeightKg, decimal? MinStockLevel, decimal? MaxStockLevel);
