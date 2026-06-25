using System.Net.Http.Json;
using GeorgiaERP.Application.Payments;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Payments;

public class TbcPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<TbcPaymentGateway> _logger;

    public PaymentProvider Provider => PaymentProvider.TbcBank;

    public TbcPaymentGateway(HttpClient httpClient, IConfiguration configuration, ILogger<TbcPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Payments:TBC:BaseUrl"] ?? "https://api.tbcbank.ge/v1";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(decimal amount, string currency, Guid orderId, string returnUrl)
    {
        try
        {
            var request = new { amount, currency, externalOrderId = orderId.ToString(), returnurl = returnUrl };
            var response = await _httpClient.PostAsJsonAsync("/payments", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return PaymentInitResult.Failure($"TBC API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<TbcPaymentResponse>();
            return PaymentInitResult.Success(result?.PayId ?? "", result?.RedirectUrl ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate TBC payment for order {OrderId}", orderId);
            return PaymentInitResult.Failure($"Connection error: {ex.Message}");
        }
    }

    public async Task<PaymentStatusResult> CheckStatusAsync(string externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/payments/{externalId}");
            if (!response.IsSuccessStatusCode)
                return new PaymentStatusResult(PaymentStatus.Failed, externalId, "Status check failed");

            var result = await response.Content.ReadFromJsonAsync<TbcPaymentResponse>();
            var status = result?.Status switch
            {
                "Succeeded" => PaymentStatus.Completed,
                "Processing" => PaymentStatus.Processing,
                "Failed" => PaymentStatus.Failed,
                "Refunded" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
            return new PaymentStatusResult(status, externalId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check TBC payment status for {ExternalId}", externalId);
            return new PaymentStatusResult(PaymentStatus.Failed, externalId, ex.Message);
        }
    }

    public async Task<RefundResult> RefundAsync(string externalId, decimal amount)
    {
        try
        {
            var request = new { amount };
            var response = await _httpClient.PostAsJsonAsync($"/payments/{externalId}/cancel", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return RefundResult.Failure($"TBC refund error: {error}");
            }

            return RefundResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund TBC payment {ExternalId}", externalId);
            return RefundResult.Failure($"Connection error: {ex.Message}");
        }
    }

    private record TbcPaymentResponse(string? PayId, string? Status, string? RedirectUrl);
}
