namespace GeorgiaERP.Application.Inventory.DTOs;

public record StockLevelDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    Guid? VariantId,
    Guid WarehouseId,
    string? WarehouseName,
    string? LocationCode,
    decimal QuantityOnHand,
    decimal QuantityReserved,
    decimal QuantityInTransit,
    decimal AvailableQty,
    decimal CostPrice,
    DateTimeOffset? LastCountDate,
    DateTimeOffset UpdatedAt);

public record StockMovementDto(
    Guid Id,
    string MovementType,
    Guid ProductId,
    string? ProductName,
    Guid? VariantId,
    Guid WarehouseId,
    string? WarehouseName,
    decimal Quantity,
    decimal CostPrice,
    string? ReferenceType,
    Guid? ReferenceId,
    string? BatchNumber,
    string? SerialNumber,
    DateTimeOffset? ExpiryDate,
    string? Notes,
    DateTimeOffset CreatedAt,
    Guid CreatedBy);

public record TransferOrderDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string? SourceWarehouseName,
    Guid DestWarehouseId,
    string? DestWarehouseName,
    string Status,
    Guid? RsGeWaybillId,
    Guid RequestedBy,
    DateTimeOffset CreatedAt);

public record TransferOrderDetailDto(
    Guid Id,
    string TransferNumber,
    Guid SourceWarehouseId,
    string? SourceWarehouseName,
    Guid DestWarehouseId,
    string? DestWarehouseName,
    string Status,
    Guid? RsGeWaybillId,
    Guid RequestedBy,
    Guid? ApprovedBy,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? ReceivedAt,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<TransferOrderLineDto> Lines);

public record TransferOrderLineDto(
    Guid Id,
    Guid ProductId,
    string? ProductName,
    Guid? VariantId,
    decimal RequestedQty,
    decimal? ShippedQty,
    decimal? ReceivedQty,
    string? BatchNumber,
    string? SerialNumber);

public record CreateStockMovementRequest(
    string MovementType,
    Guid ProductId,
    Guid? VariantId,
    Guid WarehouseId,
    decimal Quantity,
    decimal CostPrice,
    string? ReferenceType,
    Guid? ReferenceId,
    string? BatchNumber,
    string? SerialNumber,
    DateTimeOffset? ExpiryDate,
    string? Notes);
