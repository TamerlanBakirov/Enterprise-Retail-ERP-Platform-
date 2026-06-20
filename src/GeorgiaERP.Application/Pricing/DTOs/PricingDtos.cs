namespace GeorgiaERP.Application.Pricing.DTOs;

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
