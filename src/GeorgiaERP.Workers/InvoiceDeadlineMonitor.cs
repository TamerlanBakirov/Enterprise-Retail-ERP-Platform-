using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Workers;

/// <summary>
/// Background service that monitors fiscal documents approaching their RS.GE
/// submission deadline (30 days from creation). Georgian tax law imposes a 100%
/// VAT penalty for late waybill/invoice uploads, so this monitor:
///
/// 1. Logs warnings for documents within 5 days of their deadline.
/// 2. Logs errors for documents within 1 day of their deadline.
/// 3. Re-enqueues stuck documents with elevated priority so they are processed first.
/// 4. Logs critical alerts for documents that have passed their deadline.
///
/// Runs every hour to balance responsiveness with database load.
/// </summary>
public class InvoiceDeadlineMonitor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InvoiceDeadlineMonitor> _logger;

    private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan WarningThreshold = TimeSpan.FromDays(5);
    private static readonly TimeSpan CriticalThreshold = TimeSpan.FromDays(1);

    public InvoiceDeadlineMonitor(
        IServiceScopeFactory scopeFactory,
        ILogger<InvoiceDeadlineMonitor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup to let the system stabilize.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckDeadlinesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Invoice deadline monitoring check failed");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task CheckDeadlinesAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRsGeQueuePublisher>();

        var now = DateTimeOffset.UtcNow;

        // Find non-terminal documents with approaching deadlines.
        var atRisk = await dbContext.FiscalDocuments
            .Where(d => d.SubmissionDeadline != null
                        && d.Status != FiscalDocumentStatus.Confirmed
                        && d.Status != FiscalDocumentStatus.Cancelled
                        && d.Status != FiscalDocumentStatus.Rejected
                        && d.SubmissionDeadline <= now.Add(WarningThreshold))
            .OrderBy(d => d.SubmissionDeadline)
            .Take(500)
            .Select(d => new
            {
                d.Id,
                d.DocumentType,
                d.Status,
                d.SubmissionDeadline,
                d.DocumentNumber,
                d.RetryCount,
                d.CreatedAt
            })
            .ToListAsync(cancellationToken);

        if (atRisk.Count == 0)
            return;

        var overdue = 0;
        var critical = 0;
        var warning = 0;

        foreach (var doc in atRisk)
        {
            var remaining = doc.SubmissionDeadline!.Value - now;

            if (remaining <= TimeSpan.Zero)
            {
                // OVERDUE: the 30-day window has passed -- risk of 100% VAT penalty.
                overdue++;
                _logger.LogCritical(
                    "OVERDUE: Fiscal document {DocumentId} ({DocumentType}) missed its RS.GE deadline by {OverdueHours:F1} hours. " +
                    "Status: {Status}, Created: {CreatedAt}. 100% VAT penalty risk!",
                    doc.Id, doc.DocumentType, Math.Abs(remaining.TotalHours), doc.Status, doc.CreatedAt);
            }
            else if (remaining <= CriticalThreshold)
            {
                // CRITICAL: less than 1 day remaining.
                critical++;
                _logger.LogError(
                    "CRITICAL: Fiscal document {DocumentId} ({DocumentType}) deadline in {RemainingHours:F1} hours. " +
                    "Status: {Status}, Retries: {RetryCount}",
                    doc.Id, doc.DocumentType, remaining.TotalHours, doc.Status, doc.RetryCount);

                // Re-enqueue stuck documents with critical priority.
                if (doc.Status is FiscalDocumentStatus.Queued or FiscalDocumentStatus.Failed)
                {
                    var operation = doc.DocumentType is FiscalDocumentType.Invoice or FiscalDocumentType.FiscalReceipt
                        ? RsGeOperation.SubmitInvoice
                        : RsGeOperation.SubmitWaybill;

                    await publisher.PublishAsync(
                        new RsGeSubmissionMessage { FiscalDocumentId = doc.Id, Operation = operation },
                        cancellationToken);
                }
            }
            else
            {
                // WARNING: within 5-day window.
                warning++;
                _logger.LogWarning(
                    "Fiscal document {DocumentId} ({DocumentType}) deadline approaching in {RemainingDays:F1} days. " +
                    "Status: {Status}",
                    doc.Id, doc.DocumentType, remaining.TotalDays, doc.Status);
            }
        }

        _logger.LogInformation(
            "Invoice deadline check complete: {OverdueCount} overdue, {CriticalCount} critical, {WarningCount} warning",
            overdue, critical, warning);
    }
}
