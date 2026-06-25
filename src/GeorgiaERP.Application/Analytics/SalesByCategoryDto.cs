namespace GeorgiaERP.Application.Analytics;

public record SalesByCategoryDto(
    string CategoryName,
    decimal Revenue,
    decimal Percentage);
