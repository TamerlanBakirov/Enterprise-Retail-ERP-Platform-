namespace GeorgiaERP.Application.Compliance;

/// <summary>
/// Application-layer abstraction for logging RS.GE communication requests and responses.
/// Infrastructure provides the concrete persistence implementation.
/// </summary>
public interface IRsGeCommunicationLogger
{
    Task LogRequestAsync(Guid? fiscalDocumentId, string operation, string endpoint, string requestPayload, Guid correlationId);
    Task LogResponseAsync(Guid correlationId, string responsePayload, int httpStatus, int durationMs, string? errorMessage);
}
