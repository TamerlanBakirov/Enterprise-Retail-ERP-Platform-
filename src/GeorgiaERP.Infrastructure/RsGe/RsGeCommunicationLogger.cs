using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.RsGe;

public class RsGeCommunicationLogger : IRsGeCommunicationLogger
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<RsGeCommunicationLogger> _logger;

    public RsGeCommunicationLogger(AppDbContext dbContext, ILogger<RsGeCommunicationLogger> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task LogRequestAsync(Guid? fiscalDocumentId, string operation, string endpoint, string requestPayload, Guid correlationId)
    {
        try
        {
            // Insert directly via SQL to avoid dependency on the domain entity's private constructor.
            // The RsGeCommunicationLog table stores all RS.GE SOAP communication for auditing.
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"INSERT INTO rs_ge_communication_logs (id, fiscal_document_id, operation, endpoint, request_payload, correlation_id, created_at)
                  VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6})",
                Guid.NewGuid(),
                fiscalDocumentId as object ?? DBNull.Value,
                operation,
                endpoint,
                requestPayload,
                correlationId,
                DateTimeOffset.UtcNow);

            _logger.LogDebug("Logged RS.GE request: {Operation} [{CorrelationId}]", operation, correlationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log RS.GE request: {Operation} [{CorrelationId}]", operation, correlationId);
        }
    }

    public async Task LogResponseAsync(Guid correlationId, string responsePayload, int httpStatus, int durationMs, string? errorMessage)
    {
        try
        {
            await _dbContext.Database.ExecuteSqlRawAsync(
                @"UPDATE rs_ge_communication_logs
                  SET response_payload = {0}, http_status = {1}, duration_ms = {2}, error_message = {3}
                  WHERE correlation_id = {4}",
                responsePayload,
                httpStatus,
                durationMs,
                errorMessage as object ?? DBNull.Value,
                correlationId);

            _logger.LogDebug("Logged RS.GE response: [{CorrelationId}] HTTP {HttpStatus} in {DurationMs}ms",
                correlationId, httpStatus, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log RS.GE response: [{CorrelationId}]", correlationId);
        }
    }
}
