using GeorgiaERP.Domain.Reports;

namespace GeorgiaERP.Application.Reports.DTOs;

public record ScheduledReportDto(
    Guid Id,
    string Name,
    ReportType ReportType,
    string CronExpression,
    string Recipients,
    ScheduleFormat Format,
    bool IsActive,
    DateTimeOffset? LastRunAt,
    DateTimeOffset? NextRunAt,
    DateTimeOffset CreatedAt,
    Guid CreatedBy);
