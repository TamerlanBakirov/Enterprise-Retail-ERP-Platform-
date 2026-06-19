using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.RsGe;

/// <summary>
/// Standalone audit logger for RS.GE communication, writing through the domain
/// entity and EF Core so it stays consistent with the mapped schema. The
/// submission processor logs inline within its own transaction; this logger
/// exists for fire-and-forget logging from paths that are not part of a
/// document's submission transaction.
/// </summary>
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
        if (fiscalDocumentId is null)
        {
            // The communication log is keyed to a fiscal document; without one we
            // only emit an application-level log line for traceability.
            _logger.LogDebug("RS.GE request {Operation} [{CorrelationId}] (no fiscal document)", operation, correlationId);
            return;
        }

        try
        {
            var entry = RsGeCommunicationLog.Create(fiscalDocumentId.Value, operation, CommunicationDirection.Request, endpoint);
            entry.SetRequest(requestPayload, correlationId);
            _dbContext.RsGeCommunicationLogs.Add(entry);
            await _dbContext.SaveChangesAsync();

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
            var entry = await _dbContext.RsGeCommunicationLogs
                .FirstOrDefaultAsync(l => l.CorrelationId == correlationId);

            if (entry is null)
            {
                _logger.LogWarning("No RS.GE request log found for correlation {CorrelationId}", correlationId);
                return;
            }

            entry.SetResponse(responsePayload, httpStatus, durationMs, errorMessage);
            await _dbContext.SaveChangesAsync();

            _logger.LogDebug("Logged RS.GE response: [{CorrelationId}] HTTP {HttpStatus} in {DurationMs}ms",
                correlationId, httpStatus, durationMs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log RS.GE response: [{CorrelationId}]", correlationId);
        }
    }
}
