using GeorgiaERP.Application.Compliance;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies the RS.GE SOAP endpoint is reachable by calling
/// the lightweight <c>what_is_my_ip</c> method. This method requires no
/// authentication and has minimal side effects, making it ideal for liveness probes.
/// </summary>
public sealed class RsGeSoapHealthCheck : IHealthCheck
{
    private readonly IRsGeSoapClient _soapClient;
    private readonly ILogger<RsGeSoapHealthCheck> _logger;

    public RsGeSoapHealthCheck(IRsGeSoapClient soapClient, ILogger<RsGeSoapHealthCheck> logger)
    {
        _soapClient = soapClient;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var ip = await _soapClient.GetMyIpAsync();

            if (string.IsNullOrWhiteSpace(ip))
            {
                return HealthCheckResult.Degraded(
                    "RS.GE SOAP endpoint responded but returned empty IP result");
            }

            return HealthCheckResult.Healthy(
                $"RS.GE SOAP endpoint reachable (our IP: {ip})");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning(ex, "RS.GE health check failed: HTTP error");
            return HealthCheckResult.Unhealthy(
                "RS.GE SOAP endpoint is unreachable",
                exception: ex,
                data: new Dictionary<string, object> { ["error"] = ex.Message });
        }
        catch (TaskCanceledException ex)
        {
            _logger.LogWarning(ex, "RS.GE health check timed out");
            return HealthCheckResult.Unhealthy(
                "RS.GE SOAP endpoint timed out",
                exception: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RS.GE health check encountered unexpected error");
            return HealthCheckResult.Unhealthy(
                "RS.GE SOAP endpoint check failed",
                exception: ex);
        }
    }
}
