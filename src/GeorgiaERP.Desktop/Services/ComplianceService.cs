using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IComplianceService
{
    Task<RsGeHealthDto?> CheckRsGeHealthAsync();
    Task<List<WaybillDto>> GetWaybillsAsync(int page = 1, int pageSize = 20);
    Task<WaybillDto?> CreateWaybillAsync(CreateWaybillRequest request);
    Task<ApiResult> ConfirmWaybillAsync(Guid fiscalDocumentId);
    Task<ApiResult> CloseWaybillAsync(Guid fiscalDocumentId);
    Task<List<FiscalDocumentDto>> GetFiscalDocumentsAsync(string? type = null, string? status = null, int page = 1, int pageSize = 20);
    Task<VatSummaryDto?> GetVatSummaryAsync(int? year = null, int? month = null);
    Task<DeadlinesResponse?> GetDeadlinesAsync(int warningDays = 7);
    Task<TinLookupResult?> LookupTinNameAsync(string tin);
    Task<VatStatusResult?> GetVatStatusAsync(string tin);
}

public class ComplianceService : IComplianceService
{
    private readonly IApiClient _api;
    public ComplianceService(IApiClient api) => _api = api;

    public Task<RsGeHealthDto?> CheckRsGeHealthAsync() =>
        _api.GetAsync<RsGeHealthDto>("compliance/rsge/health");

    public Task<List<WaybillDto>> GetWaybillsAsync(int page, int pageSize) =>
        _api.GetAsync<List<WaybillDto>>($"compliance/waybills?page={page}&pageSize={pageSize}")!;

    public Task<WaybillDto?> CreateWaybillAsync(CreateWaybillRequest request) =>
        _api.PostAsync<CreateWaybillRequest, WaybillDto>("compliance/waybills", request);

    public Task<ApiResult> ConfirmWaybillAsync(Guid fiscalDocumentId) =>
        _api.PostAsync($"compliance/waybills/{fiscalDocumentId}/confirm");

    public Task<ApiResult> CloseWaybillAsync(Guid fiscalDocumentId) =>
        _api.PostAsync($"compliance/waybills/{fiscalDocumentId}/close");

    public Task<List<FiscalDocumentDto>> GetFiscalDocumentsAsync(string? type, string? status, int page, int pageSize)
    {
        var q = $"compliance/fiscal-documents?page={page}&pageSize={pageSize}";
        if (!string.IsNullOrEmpty(type)) q += $"&type={type}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return _api.GetAsync<List<FiscalDocumentDto>>(q)!;
    }

    public Task<VatSummaryDto?> GetVatSummaryAsync(int? year, int? month)
    {
        var q = "compliance/vat-summary";
        var sep = '?';
        if (year.HasValue) { q += $"{sep}year={year}"; sep = '&'; }
        if (month.HasValue) { q += $"{sep}month={month}"; }
        return _api.GetAsync<VatSummaryDto>(q);
    }

    public Task<DeadlinesResponse?> GetDeadlinesAsync(int warningDays) =>
        _api.GetAsync<DeadlinesResponse>($"compliance/deadlines?warningDays={warningDays}");

    public Task<TinLookupResult?> LookupTinNameAsync(string tin) =>
        _api.GetAsync<TinLookupResult>($"compliance/rsge/tin/{tin}/name");

    public Task<VatStatusResult?> GetVatStatusAsync(string tin) =>
        _api.GetAsync<VatStatusResult>($"compliance/rsge/tin/{tin}/vat-status");
}
