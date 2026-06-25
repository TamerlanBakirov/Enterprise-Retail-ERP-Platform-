using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IReportService
{
    Task<SalesReportDto?> GetSalesReportAsync(Guid? storeId, DateTimeOffset from, DateTimeOffset to);
    Task<StockReportDto?> GetStockReportAsync(Guid? warehouseId = null);
    Task<VatReportDto?> GetVatReportAsync(int? year = null, int? month = null);
    Task<ProfitMarginReportDto?> GetProfitMarginAsync(DateTimeOffset from, DateTimeOffset to, Guid? storeId = null);
    Task<TopSellingProductsReportDto?> GetTopSellingAsync(DateTimeOffset from, DateTimeOffset to, Guid? storeId = null, int top = 20, string sortBy = "Revenue");
    Task<SupplierPerformanceReportDto?> GetSupplierPerformanceAsync(DateTimeOffset from, DateTimeOffset to, Guid? supplierId = null);
}

public class ReportService : IReportService
{
    private readonly IApiClient _api;
    public ReportService(IApiClient api) => _api = api;

    public Task<SalesReportDto?> GetSalesReportAsync(Guid? storeId, DateTimeOffset from, DateTimeOffset to)
    {
        var q = $"reports/sales?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
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

    public Task<ProfitMarginReportDto?> GetProfitMarginAsync(DateTimeOffset from, DateTimeOffset to, Guid? storeId)
    {
        var q = $"reports/profit-margin?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        if (storeId.HasValue) q += $"&storeId={storeId}";
        return _api.GetAsync<ProfitMarginReportDto>(q);
    }

    public Task<TopSellingProductsReportDto?> GetTopSellingAsync(DateTimeOffset from, DateTimeOffset to, Guid? storeId, int top, string sortBy)
    {
        var q = $"reports/top-selling?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}&top={top}&sortBy={sortBy}";
        if (storeId.HasValue) q += $"&storeId={storeId}";
        return _api.GetAsync<TopSellingProductsReportDto>(q);
    }

    public Task<SupplierPerformanceReportDto?> GetSupplierPerformanceAsync(DateTimeOffset from, DateTimeOffset to, Guid? supplierId)
    {
        var q = $"reports/supplier-performance?from={Uri.EscapeDataString(from.ToString("O"))}&to={Uri.EscapeDataString(to.ToString("O"))}";
        if (supplierId.HasValue) q += $"&supplierId={supplierId}";
        return _api.GetAsync<SupplierPerformanceReportDto>(q);
    }
}
