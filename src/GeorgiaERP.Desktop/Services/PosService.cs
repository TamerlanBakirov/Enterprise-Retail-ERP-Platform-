using GeorgiaERP.Desktop.Models;

namespace GeorgiaERP.Desktop.Services;

public interface IPosService
{
    Task<PosSessionDto?> OpenSessionAsync(OpenPosSessionRequest request);
    Task<ApiResult> CloseSessionAsync(Guid sessionId, ClosePosSessionRequest request);
    Task<PagedResult<PosSessionDto>> GetSessionsAsync(Guid? terminalId = null, string? status = null, int page = 1, int pageSize = 20);
    Task<PosTransactionDto?> CreateTransactionAsync(CreatePosTransactionRequest request);
    Task<PagedResult<PosTransactionDto>> GetTransactionsAsync(Guid? sessionId = null, Guid? storeId = null, string? status = null, DateTimeOffset? from = null, DateTimeOffset? to = null, int page = 1, int pageSize = 20);
    Task<PosTransactionDetailDto?> GetTransactionDetailAsync(Guid transactionId);
    Task<ApiResult> VoidTransactionAsync(Guid transactionId, string reason);
}

public class PosService : IPosService
{
    private readonly IApiClient _api;
    public PosService(IApiClient api) => _api = api;

    public Task<PosSessionDto?> OpenSessionAsync(OpenPosSessionRequest request) =>
        _api.PostAsync<OpenPosSessionRequest, PosSessionDto>("pos/sessions", request);

    public Task<ApiResult> CloseSessionAsync(Guid sessionId, ClosePosSessionRequest request) =>
        _api.PostAsync($"pos/sessions/{sessionId}/close", request);

    public Task<PagedResult<PosSessionDto>> GetSessionsAsync(Guid? terminalId, string? status, int page, int pageSize)
    {
        var q = $"pos/sessions?page={page}&pageSize={pageSize}";
        if (terminalId.HasValue) q += $"&terminalId={terminalId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        return _api.GetAsync<PagedResult<PosSessionDto>>(q)!;
    }

    public Task<PosTransactionDto?> CreateTransactionAsync(CreatePosTransactionRequest request) =>
        _api.PostAsync<CreatePosTransactionRequest, PosTransactionDto>("pos/transactions", request);

    public Task<PagedResult<PosTransactionDto>> GetTransactionsAsync(Guid? sessionId, Guid? storeId, string? status, DateTimeOffset? from, DateTimeOffset? to, int page, int pageSize)
    {
        var q = $"pos/transactions?page={page}&pageSize={pageSize}";
        if (sessionId.HasValue) q += $"&sessionId={sessionId}";
        if (storeId.HasValue) q += $"&storeId={storeId}";
        if (!string.IsNullOrEmpty(status)) q += $"&status={status}";
        if (from.HasValue) q += $"&from={from.Value:O}";
        if (to.HasValue) q += $"&to={to.Value:O}";
        return _api.GetAsync<PagedResult<PosTransactionDto>>(q)!;
    }

    public Task<PosTransactionDetailDto?> GetTransactionDetailAsync(Guid transactionId) =>
        _api.GetAsync<PosTransactionDetailDto>($"pos/transactions/{transactionId}");

    public Task<ApiResult> VoidTransactionAsync(Guid transactionId, string reason) =>
        _api.PostAsync($"pos/transactions/{transactionId}/void", new { reason });
}
