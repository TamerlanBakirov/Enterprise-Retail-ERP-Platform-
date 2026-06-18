namespace GeorgiaERP.Infrastructure.RsGe;

public interface IRsGeCommunicationLogger
{
    Task LogRequestAsync(Guid? fiscalDocumentId, string operation, string endpoint, string requestPayload, Guid correlationId);
    Task LogResponseAsync(Guid correlationId, string responsePayload, int httpStatus, int durationMs, string? errorMessage);
}
