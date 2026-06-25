using System.Net.Http.Json;
using GeorgiaERP.Application.Payments;
using GeorgiaERP.Application.Payments.DTOs;
using GeorgiaERP.Domain.Payments;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Payments;

public class BogPaymentGateway : IPaymentGateway
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<BogPaymentGateway> _logger;

    public PaymentProvider Provider => PaymentProvider.BankOfGeorgia;

    public BogPaymentGateway(HttpClient httpClient, IConfiguration configuration, ILogger<BogPaymentGateway> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;

        var baseUrl = _configuration["Payments:BOG:BaseUrl"] ?? "https://api.bog.ge/payments/v1";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<PaymentInitResult> InitiatePaymentAsync(decimal amount, string currency, Guid orderId, string returnUrl)
    {
        try
        {
            var request = new { amount, currency, order_id = orderId.ToString(), return_url = returnUrl };
            var response = await _httpClient.PostAsJsonAsync("/orders", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return PaymentInitResult.Failure($"BOG API error: {error}");
            }

            var result = await response.Content.ReadFromJsonAsync<BogOrderResponse>();
            return PaymentInitResult.Success(result?.Id ?? "", result?.RedirectUrl ?? "");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate BOG payment for order {OrderId}", orderId);
            return PaymentInitResult.Failure($"Connection error: {ex.Message}");
        }
    }

    public async Task<PaymentStatusResult> CheckStatusAsync(string externalId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/orders/{externalId}");
            if (!response.IsSuccessStatusCode)
                return new PaymentStatusResult(PaymentStatus.Failed, externalId, "Status check failed");

            var result = await response.Content.ReadFromJsonAsync<BogOrderResponse>();
            var status = result?.Status switch
            {
                "completed" => PaymentStatus.Completed,
                "processing" => PaymentStatus.Processing,
                "failed" => PaymentStatus.Failed,
                "refunded" => PaymentStatus.Refunded,
                _ => PaymentStatus.Pending
            };
            return new PaymentStatusResult(status, externalId, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to check BOG payment status for {ExternalId}", externalId);
            return new PaymentStatusResult(PaymentStatus.Failed, externalId, ex.Message);
        }
    }

    public async Task<RefundResult> RefundAsync(string externalId, decimal amount)
    {
        try
        {
            var request = new { amount };
            var response = await _httpClient.PostAsJsonAsync($"/orders/{externalId}/refund", request);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                return RefundResult.Failure($"BOG refund error: {error}");
            }

            return RefundResult.Success();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to refund BOG payment {ExternalId}", externalId);
            return RefundResult.Failure($"Connection error: {ex.Message}");
        }
    }

    private record BogOrderResponse(string? Id, string? Status, string? RedirectUrl);
}
