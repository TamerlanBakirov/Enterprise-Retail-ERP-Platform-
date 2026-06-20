using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IReportService
{
    Task<SalesReportDto?> GetSalesReportAsync(Guid? storeId, DateTimeOffset from, DateTimeOffset to);
    Task<StockReportDto?> GetStockReportAsync(Guid? warehouseId = null);
    Task<VatReportDto?> GetVatReportAsync(int? year = null, int? month = null);
}

public class ReportService : IReportService
{
    private readonly IApiClient _api;
    public ReportService(IApiClient api) => _api = api;

    public Task<SalesReportDto?> GetSalesReportAsync(Guid? storeId, DateTimeOffset from, DateTimeOffset to)
    {
        var q = $"reports/sales?from={from:O}&to={to:O}";
        if (storeId.HasValue) q += $"&storeId={storeId}";
        return _api.GetAsync<SalesReportDto>(q);
    }

    public Task<StockReportDto?> GetStockReportAsync(Guid? warehouseId)
    {
        var q = "reports/stock";
        if (warehouseId.HasValue) q += $"?warehouseId={warehouseId}";
        return _api.GetAsync<StockReportDto>(q);
    }

    public Task<VatReportDto?> GetVatReportAsync(int? year, int? month)
    {
        var q = "reports/vat";
        var parts = new List<string>();
        if (year.HasValue) parts.Add($"year={year}");
        if (month.HasValue) parts.Add($"month={month}");
        if (parts.Count > 0) q += "?" + string.Join("&", parts);
        return _api.GetAsync<VatReportDto>(q);
    }
}
