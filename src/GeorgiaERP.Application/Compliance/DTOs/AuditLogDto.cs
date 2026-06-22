namespace GeorgiaERP.Application.Compliance.DTOs;

public record AuditLogDto(
    Guid Id,
    string EntityType,
    string EntityId,
    string Action,
    string? OldValues,
    string? NewValues,
    string? ChangedProperties,
    Guid? UserId,
    string? IpAddress,
    DateTimeOffset Timestamp);
