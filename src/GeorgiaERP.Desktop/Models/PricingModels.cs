namespace GeorgiaERP.Desktop.Models;

public record PriceListDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string Currency,
    string PriceType,
    Guid? StoreId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int Priority,
    int ItemCount,
    DateTimeOffset CreatedAt);

public record PriceListItemDto(
    Guid Id,
    Guid PriceListId,
    Guid ProductId,
    string? ProductName,
    Guid? VariantId,
    decimal Price,
    decimal MinQty);

public record PromotionDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string PromotionType,
    decimal? DiscountValue,
    string? Conditions,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    bool IsActive,
    int? MaxUses,
    int CurrentUses,
    DateTimeOffset CreatedAt);

public record CreatePriceListRequest(
    string Code,
    string Name,
    string? NameKa,
    string PriceType,
    Guid? StoreId,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int Priority);

public record SetPriceRequest(
    Guid PriceListId,
    Guid ProductId,
    decimal Price,
    decimal MinQty,
    Guid? VariantId);

public record CreatePromotionRequest(
    string Code,
    string Name,
    string? NameKa,
    string PromotionType,
    decimal? DiscountValue,
    string? Conditions,
    DateTimeOffset ValidFrom,
    DateTimeOffset? ValidTo,
    int? MaxUses);
