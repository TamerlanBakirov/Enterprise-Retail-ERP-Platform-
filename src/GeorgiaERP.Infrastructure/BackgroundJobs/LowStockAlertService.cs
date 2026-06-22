using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically scans stock levels against product reorder
/// points and generates log-based alerts when items fall below their minimum stock level.
///
/// Alert severity:
///   - Warning: quantity on hand is at or below MinStockLevel but above zero
///   - Critical: quantity on hand is zero or negative (out of stock)
///
/// Configurable via appsettings:
///   "LowStockAlert": {
///     "IntervalMinutes": 30,
///     "BatchSize": 500,
///     "NotificationEmail": "inventory@example.com"
///   }
/// </summary>
public sealed class LowStockAlertService : BackgroundService
{
    private const string JobName = "LowStockAlert";

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<LowStockAlertService> _logger;
    private readonly INotificationService? _notificationService;
    private readonly IBackgroundJobRegistry _jobRegistry;
    private readonly TimeSpan _interval;
    private readonly int _batchSize;
    private readonly string? _notificationEmail;

    public LowStockAlertService(
        IServiceScopeFactory scopeFactory,
        ILogger<LowStockAlertService> logger,
        IConfiguration configuration,
        IBackgroundJobRegistry jobRegistry,
        INotificationService? notificationService = null)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _jobRegistry = jobRegistry;
        _notificationService = notificationService;

        var section = configuration.GetSection("LowStockAlert");
        _interval = TimeSpan.FromMinutes(section.GetValue("IntervalMinutes", 30));
        _batchSize = section.GetValue("BatchSize", 500);
        _notificationEmail = section.GetValue<string?>("NotificationEmail");

        _jobRegistry.Register(JobName,
            "Scans stock levels and alerts when items fall below reorder points",
            _interval);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Stagger startup to let the application stabilize.
        try
        {
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        _logger.LogInformation(
            "Low stock alert service started. Scan interval: {IntervalMinutes}min, Batch size: {BatchSize}",
            _interval.TotalMinutes, _batchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _jobRegistry.MarkRunning(JobName);
                await ScanStockLevelsAsync(stoppingToken);
                _jobRegistry.RecordSuccess(JobName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Low stock alert scan failed");
                _jobRegistry.RecordFailure(JobName, ex.Message);
            }

            try
            {
                await Task.Delay(_interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task ScanStockLevelsAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // Query stock levels joined with products that have a reorder point defined
        var lowStockItems = await (
            from s in dbContext.StockLevels.AsNoTracking()
            join p in dbContext.Products.AsNoTracking() on s.ProductId equals p.Id
            join w in dbContext.Warehouses.AsNoTracking() on s.WarehouseId equals w.Id
            where p.MinStockLevel.HasValue && p.IsActive
                  && s.QuantityOnHand <= p.MinStockLevel.Value
            orderby s.QuantityOnHand ascending
            select new
            {
                p.Sku,
                ProductName = p.Name,
                WarehouseName = w.Name,
                s.QuantityOnHand,
                p.MinStockLevel,
                s.ProductId,
                s.WarehouseId
            })
            .Take(_batchSize)
            .ToListAsync(cancellationToken);

        if (lowStockItems.Count == 0)
        {
            _logger.LogDebug("Low stock scan completed: no items below reorder point");
            return;
        }

        var outOfStock = 0;
        var belowMinimum = 0;

        foreach (var item in lowStockItems)
        {
            if (item.QuantityOnHand <= 0)
            {
                outOfStock++;
                _logger.LogError(
                    "OUT OF STOCK: {Sku} ({ProductName}) at {Warehouse}. " +
                    "Qty: {Qty}, Min: {Min}",
                    item.Sku, item.ProductName, item.WarehouseName,
                    item.QuantityOnHand, item.MinStockLevel);
            }
            else
            {
                belowMinimum++;
                _logger.LogWarning(
                    "LOW STOCK: {Sku} ({ProductName}) at {Warehouse}. " +
                    "Qty: {Qty}, Min: {Min}",
                    item.Sku, item.ProductName, item.WarehouseName,
                    item.QuantityOnHand, item.MinStockLevel);
            }
        }

        _logger.LogInformation(
            "Low stock scan completed: {OutOfStock} out of stock, {BelowMinimum} below minimum, " +
            "{Total} total items flagged",
            outOfStock, belowMinimum, lowStockItems.Count);

        // Send email notification if configured
        if (!string.IsNullOrWhiteSpace(_notificationEmail))
        {
            try
            {
                var emailService = scope.ServiceProvider.GetService<IEmailService>();
                if (emailService is not null)
                {
                    var emailItems = lowStockItems.Select(i => new LowStockItem(
                        i.Sku, i.ProductName, i.WarehouseName,
                        i.QuantityOnHand, i.MinStockLevel ?? 0)).ToList();

                    var email = EmailTemplates.LowStockAlert(_notificationEmail, emailItems);
                    await emailService.SendAsync(email, cancellationToken);

                    _logger.LogInformation("Low stock alert email sent to {Email}", _notificationEmail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to send low stock alert email to {Email}", _notificationEmail);
            }
        }

        // Push real-time notification via SignalR
        if (_notificationService is not null)
        {
            try
            {
                var payload = new NotificationPayload
                {
                    EventType = NotificationEvents.LowStockAlert,
                    Title = "Low Stock Alert",
                    Message = $"{outOfStock} items out of stock, {belowMinimum} items below minimum level",
                    Severity = outOfStock > 0 ? "critical" : "warning",
                    Data = new
                    {
                        OutOfStockCount = outOfStock,
                        BelowMinimumCount = belowMinimum,
                        TotalFlagged = lowStockItems.Count,
                        Items = lowStockItems.Take(20).Select(i => new
                        {
                            i.Sku,
                            i.ProductName,
                            i.WarehouseName,
                            i.QuantityOnHand,
                            MinStockLevel = i.MinStockLevel ?? 0
                        })
                    }
                };

                await _notificationService.SendToGroupAsync(
                    "role-admin", NotificationEvents.LowStockAlert, payload, cancellationToken);
                await _notificationService.SendToGroupAsync(
                    "inventory-alerts", NotificationEvents.LowStockAlert, payload, cancellationToken);

                _logger.LogDebug("Low stock SignalR notification dispatched");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to dispatch low stock SignalR notification");
            }
        }
    }
}
