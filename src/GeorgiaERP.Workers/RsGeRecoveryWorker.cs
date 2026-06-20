using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace GeorgiaERP.Workers;

/// <summary>
/// Safety net for the submission pipeline. Periodically re-enqueues fiscal
/// documents that are stuck — either queued but never published (broker was
/// down at creation time) or failed transiently and still within their retry
/// budget. This guarantees eventual submission without relying on the original
/// publish succeeding, which matters because RS.GE non-submission within 30 days
/// triggers a 100% VAT penalty.
/// </summary>
public class RsGeRecoveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly RsGeQueueOptions _options;
    private readonly ILogger<RsGeRecoveryWorker> _logger;

    private static readonly TimeSpan SweepInterval = TimeSpan.FromMinutes(5);

    // Only re-drive documents that have been idle a while, so we don't race the
    // normal queue path for freshly created documents.
    private static readonly TimeSpan StuckThreshold = TimeSpan.FromMinutes(2);

    public RsGeRecoveryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<RsGeQueueOptions> options,
        ILogger<RsGeRecoveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger the first sweep so startup isn't contended with the consumer.
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SweepAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "RS.GE recovery sweep failed");
            }

            try
            {
                await Task.Delay(SweepInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task SweepAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var publisher = scope.ServiceProvider.GetRequiredService<IRsGeQueuePublisher>();

        var cutoff = DateTimeOffset.UtcNow - StuckThreshold;

        var stuck = await dbContext.FiscalDocuments
            .Where(d => (d.Status == FiscalDocumentStatus.Queued || d.Status == FiscalDocumentStatus.Failed)
                        && d.RetryCount < _options.MaxRetryCount
                        && d.UpdatedAt < cutoff)
            .OrderBy(d => d.UpdatedAt)
            .Take(100)
            .Select(d => new { d.Id, d.DocumentType })
            .ToListAsync(cancellationToken);

        if (stuck.Count == 0)
            return;

        _logger.LogInformation("Recovery sweep re-enqueuing {Count} stuck fiscal document(s)", stuck.Count);

        foreach (var doc in stuck)
        {
            var operation = doc.DocumentType is FiscalDocumentType.Invoice or FiscalDocumentType.FiscalReceipt
                ? RsGeOperation.SubmitInvoice
                : RsGeOperation.SubmitWaybill;

            await publisher.PublishAsync(
                new RsGeSubmissionMessage { FiscalDocumentId = doc.Id, Operation = operation },
                cancellationToken);
        }
    }
}
