namespace GeorgiaERP.Application.Analytics;

public record TopProductDto(
    Guid ProductId,
    string Name,
    string Sku,
    decimal QuantitySold,
    decimal Revenue);
