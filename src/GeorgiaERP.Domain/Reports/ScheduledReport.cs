using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.Reports;

public enum ReportType
{
    DailySales,
    WeeklyStock,
    MonthlyVat,
    MonthlyPnL
}

public enum ScheduleFormat
{
    PDF,
    Excel
}

public class ScheduledReport : BaseEntity
{
    public string Name { get; private set; } = default!;
    public ReportType ReportType { get; private set; }
    public string CronExpression { get; private set; } = default!;
    public string Recipients { get; private set; } = default!;
    public ScheduleFormat Format { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset? LastRunAt { get; private set; }
    public DateTimeOffset? NextRunAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    private ScheduledReport() { }

    public static ScheduledReport Create(
        string name,
        ReportType reportType,
        string cronExpression,
        string recipients,
        ScheduleFormat format,
        Guid createdBy,
        DateTimeOffset? nextRunAt = null)
    {
        return new ScheduledReport
        {
            Name = name,
            ReportType = reportType,
            CronExpression = cronExpression,
            Recipients = recipients,
            Format = format,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy,
            NextRunAt = nextRunAt
        };
    }

    public void Update(
        string name,
        ReportType reportType,
        string cronExpression,
        string recipients,
        ScheduleFormat format)
    {
        Name = name;
        ReportType = reportType;
        CronExpression = cronExpression;
        Recipients = recipients;
        Format = format;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public void MarkExecuted(DateTimeOffset executedAt, DateTimeOffset? nextRun)
    {
        LastRunAt = executedAt;
        NextRunAt = nextRun;
    }

    public void SetNextRunAt(DateTimeOffset? nextRun)
    {
        NextRunAt = nextRun;
    }
}
