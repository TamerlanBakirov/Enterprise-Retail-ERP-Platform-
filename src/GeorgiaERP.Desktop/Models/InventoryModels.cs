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
