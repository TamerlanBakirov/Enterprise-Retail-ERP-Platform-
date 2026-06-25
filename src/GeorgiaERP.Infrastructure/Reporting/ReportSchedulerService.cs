using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Reports;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Reporting;

public class ReportSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ReportSchedulerService> _logger;

    public ReportSchedulerService(IServiceProvider serviceProvider, ILogger<ReportSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueReportsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing scheduled reports");
            }

            await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
        }
    }

    private async Task ProcessDueReportsAsync(CancellationToken ct)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTimeOffset.UtcNow;
        var activeReports = await dbContext.ScheduledReports
            .Where(r => r.IsActive)
            .ToListAsync(ct);
        var dueReports = activeReports
            .Where(r => r.NextRunAt != null && r.NextRunAt <= now)
            .ToList();

        foreach (var report in dueReports)
        {
            try
            {
                _logger.LogInformation("Executing scheduled report: {Name} ({Type})", report.Name, report.ReportType);

                var emailService = scope.ServiceProvider.GetService<IEmailService>();
                if (emailService is not null)
                {
                    var recipients = report.Recipients.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                    foreach (var recipient in recipients)
                    {
                        await emailService.SendAsync(new EmailMessage(
                            recipient,
                            $"Scheduled Report: {report.Name}",
                            $"<p>Your scheduled {report.ReportType} report has been generated.</p>"), ct);
                    }
                }

                var nextRun = SimpleCronParser.GetNextOccurrence(report.CronExpression, now);
                report.MarkExecuted(now, nextRun);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute report: {Name}", report.Name);
            }
        }

        if (dueReports.Count > 0)
            await dbContext.SaveChangesAsync(ct);
    }
}
