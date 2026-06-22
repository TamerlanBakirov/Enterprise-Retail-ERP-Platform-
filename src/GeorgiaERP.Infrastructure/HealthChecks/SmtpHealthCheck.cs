using System.Net.Sockets;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.HealthChecks;

/// <summary>
/// Checks SMTP server connectivity by opening a TCP connection to the configured
/// mail host and port. Does not authenticate or send mail.
/// </summary>
public class SmtpHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SmtpHealthCheck> _logger;

    public SmtpHealthCheck(IConfiguration configuration, ILogger<SmtpHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        var enabled = _configuration.GetValue("Email:Smtp:Enabled", false);
        if (!enabled)
        {
            return HealthCheckResult.Healthy("SMTP is disabled in configuration.");
        }

        var host = _configuration["Email:Smtp:Host"];
        var port = _configuration.GetValue("Email:Smtp:Port", 587);

        if (string.IsNullOrWhiteSpace(host))
        {
            return HealthCheckResult.Degraded("SMTP host is not configured.");
        }

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            await client.ConnectAsync(host, port, cts.Token);

            return HealthCheckResult.Healthy($"SMTP server {host}:{port} is reachable.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "SMTP health check failed for {Host}:{Port}", host, port);
            return HealthCheckResult.Degraded(
                $"Cannot connect to SMTP server {host}:{port}: {ex.Message}");
        }
    }
}
