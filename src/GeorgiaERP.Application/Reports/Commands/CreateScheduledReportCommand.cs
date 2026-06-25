using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Reports.DTOs;
using GeorgiaERP.Domain.Reports;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Reports.Commands;

public record CreateScheduledReportCommand(
    string Name,
    ReportType ReportType,
    string CronExpression,
    string Recipients,
    ScheduleFormat Format,
    Guid CreatedBy) : IRequest<Result<ScheduledReportDto>>;

public class CreateScheduledReportCommandHandler : IRequestHandler<CreateScheduledReportCommand, Result<ScheduledReportDto>>
{
    private readonly IAppDbContext _dbContext;

    public CreateScheduledReportCommandHandler(IAppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<Result<ScheduledReportDto>> Handle(CreateScheduledReportCommand request, CancellationToken cancellationToken)
    {
        var nameExists = await _dbContext.ScheduledReports
            .AnyAsync(r => r.Name == request.Name && r.IsActive, cancellationToken);

        if (nameExists)
            return Result.Conflict<ScheduledReportDto>($"A scheduled report with name '{request.Name}' already exists.");

        var report = ScheduledReport.Create(
            name: request.Name,
            reportType: request.ReportType,
            cronExpression: request.CronExpression,
            recipients: request.Recipients,
            format: request.Format,
            createdBy: request.CreatedBy);

        _dbContext.ScheduledReports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new ScheduledReportDto(
            report.Id,
            report.Name,
            report.ReportType,
            report.CronExpression,
            report.Recipients,
            report.Format,
            report.IsActive,
            report.LastRunAt,
            report.NextRunAt,
            report.CreatedAt,
            report.CreatedBy);

        return Result.Success(dto);
    }
}
