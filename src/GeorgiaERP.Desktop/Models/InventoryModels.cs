namespace GeorgiaERP.Desktop.Models;

public record StockLevelDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    string Sku,
    Guid WarehouseId,
    string WarehouseName,
    decimal Quantity,
    decimal ReorderPoint,
    decimal ReorderQuantity,
    bool IsLowStock);

public record StockMovementDto(
    Guid Id,
    Guid ProductId,
    string ProductName,
    Guid WarehouseId,
    string WarehouseName,
    string MovementType,
    decimal Quantity,
    string? Reference,
    string? Notes,
    DateTimeOffset CreatedAt);

public record TransferOrderDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string SourceWarehouseName,
    Guid DestinationWarehouseId,
    string DestinationWarehouseName,
    string Status,
    string? Notes,
    DateTimeOffset CreatedAt);

public record StockCountDto(
    Guid Id,
    string CountNumber,
    Guid WarehouseId,
    string WarehouseName,
    string Status,
    int TotalLines,
    int CountedLines,
    DateTimeOffset CreatedAt);

public record TransferOrderDetailDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string? SourceWarehouseName,
    Guid DestWarehouseId,
    string? DestWarehouseName,
    string Status,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? ReceivedAt,
    string? Notes,
    DateTimeOffset CreatedAt,
    List<TransferOrderLineDto>? Lines = null);

public record TransferOrderLineDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    decimal RequestedQty,
    decimal? ShippedQty,
    decimal? ReceivedQty);

public record StockCountDetailDto(
    Guid Id,
    Guid WarehouseId,
    string? WarehouseName,
    string CountType,
    string Status,
    decimal TotalVariance,
    int LinesWithVariance,
    DateTimeOffset CreatedAt,
    List<StockCountLineDto>? Lines = null);

public record StockCountLineDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    decimal ExpectedQty,
    decimal? CountedQty,
    decimal Variance);

public record AdjustStockRequest(
    Guid ProductId,
    Guid WarehouseId,
    decimal NewQuantity,
    string Reason);

public record CreateTransferOrderRequest(
    Guid SourceWarehouseId,
    Guid DestinationWarehouseId,
    List<TransferLineInput> Lines,
    string? Notes);

public record TransferLineInput(
    Guid ProductId,
    decimal Quantity);

public record CreateStockCountRequest(
    Guid WarehouseId,
    string CountType,
    Guid? ProductId);
