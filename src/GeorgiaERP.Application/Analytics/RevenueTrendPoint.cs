namespace GeorgiaERP.Application.Analytics;

public record RevenueTrendPoint(
    string Date,
    decimal Revenue,
    int OrderCount);
