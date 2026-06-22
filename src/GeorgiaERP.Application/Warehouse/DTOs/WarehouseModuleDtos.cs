namespace GeorgiaERP.Application.Warehouse.DTOs;

public record WarehouseDetailDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string WarehouseType,
    string? Address,
    string? City,
    string? Region,
    Guid? LinkedStoreId,
    bool IsActive,
    DateTimeOffset CreatedAt,
    int LocationCount);

public record WarehouseLocationDto(
    Guid Id,
    Guid WarehouseId,
    string Code,
    string Name,
    string? NameKa,
    string LocationType,
    Guid? ParentLocationId,
    int SortOrder,
    bool IsActive,
    int? MaxCapacity,
    string? Notes,
    DateTimeOffset CreatedAt);

public record ReceivingOrderDto(
    Guid Id,
    string ReceivingNumber,
    Guid WarehouseId,
    string Status,
    string Source,
    Guid? SourceOrderId,
    Guid? SupplierId,
    DateTimeOffset? ExpectedDate,
    DateTimeOffset? ReceivedAt,
    Guid? ReceivedBy,
    Guid? LocationId,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ReceivingOrderLineDto> Lines);

public record ReceivingOrderLineDto(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    decimal ExpectedQty,
    decimal ReceivedQty,
    decimal? DamagedQty,
    string? BatchNumber,
    string? SerialNumber,
    Guid? LocationId,
    string? Notes);

public record ShippingOrderDto(
    Guid Id,
    string ShippingNumber,
    Guid WarehouseId,
    string Status,
    string OrderType,
    Guid? SourceOrderId,
    Guid? CustomerId,
    Guid? DestWarehouseId,
    string? ShippingAddress,
    string? TrackingNumber,
    string? Carrier,
    DateTimeOffset? ExpectedShipDate,
    DateTimeOffset? ShippedAt,
    DateTimeOffset? DeliveredAt,
    Guid? ShippedBy,
    Guid? RsGeWaybillId,
    string? Notes,
    DateTimeOffset CreatedAt,
    IReadOnlyList<ShippingOrderLineDto> Lines);

public record ShippingOrderLineDto(
    Guid Id,
    Guid ProductId,
    Guid? VariantId,
    decimal OrderedQty,
    decimal PickedQty,
    decimal PackedQty,
    decimal ShippedQty,
    Guid? PickLocationId,
    string? BatchNumber,
    string? SerialNumber,
    string? Notes);
