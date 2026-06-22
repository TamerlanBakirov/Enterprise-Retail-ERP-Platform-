using System.Diagnostics;
using GeorgiaERP.Application.Compliance;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;
using Polly.Timeout;

namespace GeorgiaERP.Infrastructure.RsGe;

/// <summary>
/// Decorator that adds Polly-based resilience to the RS.GE SOAP client: retry with
/// exponential backoff and jitter, circuit breaker, and per-attempt + total timeout.
/// Also propagates a correlation ID through Activity.Current for end-to-end tracing.
/// </summary>
public sealed class ResilientRsGeSoapClient : IRsGeSoapClient
{
    private readonly IRsGeSoapClient _inner;
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger<ResilientRsGeSoapClient> _logger;

    public ResilientRsGeSoapClient(
        IRsGeSoapClient inner,
        ResiliencePipeline pipeline,
        ILogger<ResilientRsGeSoapClient> logger)
    {
        _inner = inner;
        _pipeline = pipeline;
        _logger = logger;
    }

    public Task<string> GetMyIpAsync()
        => ExecuteWithResilienceAsync("what_is_my_ip", () => _inner.GetMyIpAsync());

    public Task<RsGeServiceUser> CheckServiceUserAsync(string serviceUser, string servicePassword)
        => ExecuteWithResilienceAsync("chek_service_user", () => _inner.CheckServiceUserAsync(serviceUser, servicePassword));

    public Task<RsGeNameResult> GetNameFromTinAsync(string tin)
        => ExecuteWithResilienceAsync("get_name_from_tin", () => _inner.GetNameFromTinAsync(tin));

    public Task<bool> IsVatPayerAsync(string tin)
        => ExecuteWithResilienceAsync("is_vat_payer_tin", () => _inner.IsVatPayerAsync(tin));

    public Task<IReadOnlyList<RsGeUnit>> GetUnitsAsync()
        => ExecuteWithResilienceAsync("get_waybill_units", () => _inner.GetUnitsAsync());

    public Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync()
        => ExecuteWithResilienceAsync("get_transport_types", () => _inner.GetTransportTypesAsync());

    public Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync()
        => ExecuteWithResilienceAsync("get_waybill_types", () => _inner.GetWaybillTypesAsync());

    public Task<RsGeWaybillResult> SaveWaybillAsync(RsGeWaybillRequest request)
        => ExecuteWithResilienceAsync("save_waybill", () => _inner.SaveWaybillAsync(request));

    public Task<RsGeResult> SendWaybillAsync(int waybillId)
        => ExecuteWithResilienceAsync("send_waybill", () => _inner.SendWaybillAsync(waybillId));

    public Task<RsGeResult> ConfirmWaybillAsync(int waybillId)
        => ExecuteWithResilienceAsync("confirm_waybill", () => _inner.ConfirmWaybillAsync(waybillId));

    public Task<RsGeResult> CloseWaybillAsync(int waybillId)
        => ExecuteWithResilienceAsync("close_waybill", () => _inner.CloseWaybillAsync(waybillId));

    public Task<RsGeResult> RejectWaybillAsync(int waybillId)
        => ExecuteWithResilienceAsync("reject_waybill", () => _inner.RejectWaybillAsync(waybillId));

    public Task<RsGeWaybillData?> GetWaybillAsync(int waybillId)
        => ExecuteWithResilienceAsync("get_waybill", () => _inner.GetWaybillAsync(waybillId));

    public Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest request)
        => ExecuteWithResilienceAsync("save_invoice", () => _inner.SaveInvoiceAsync(request));

    private async Task<T> ExecuteWithResilienceAsync<T>(string operation, Func<Task<T>> action)
    {
        // Ensure a correlation ID exists for the entire call chain.
        var activity = Activity.Current;
        var correlationId = activity?.Id ?? Guid.NewGuid().ToString();

        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["RsGeOperation"] = operation,
            ["CorrelationId"] = correlationId
        });

        try
        {
            return await _pipeline.ExecuteAsync(async ct => await action(), CancellationToken.None);
        }
        catch (BrokenCircuitException ex)
        {
            _logger.LogError(ex,
                "RS.GE circuit breaker is open for operation {Operation}; failing fast",
                operation);
            throw;
        }
        catch (TimeoutRejectedException ex)
        {
            _logger.LogError(ex,
                "RS.GE operation {Operation} timed out after resilience pipeline exhausted",
                operation);
            throw new HttpRequestException($"RS.GE operation {operation} timed out", ex);
        }
    }
}
