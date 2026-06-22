using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Inventory.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Inventory.EventHandlers;

public class LowStockNotificationHandler : INotificationHandler<StockAdjustedEvent>
{
    private readonly IAppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LowStockNotificationHandler> _logger;

    public LowStockNotificationHandler(
        IAppDbContext dbContext,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<LowStockNotificationHandler> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task Handle(StockAdjustedEvent notification, CancellationToken cancellationToken)
    {
        if (notification.QuantityChange >= 0)
            return;

        var product = await _dbContext.Products
            .AsNoTracking()
            .Where(p => p.Id == notification.ProductId)
            .Select(p => new { p.Name, p.Sku, p.MinStockLevel })
            .FirstOrDefaultAsync(cancellationToken);

        if (product is null || product.MinStockLevel is null)
            return;

        if (notification.NewQuantityOnHand >= product.MinStockLevel.Value)
            return;

        var recipients = _configuration["Notifications:LowStockRecipients"];
        if (string.IsNullOrWhiteSpace(recipients))
        {
            _logger.LogWarning(
                "Low stock detected for {ProductName} (SKU: {Sku}) at {Quantity} units (min: {MinLevel}), but no recipients configured in Notifications:LowStockRecipients",
                product.Name, product.Sku, notification.NewQuantityOnHand, product.MinStockLevel.Value);
            return;
        }

        foreach (var recipient in recipients.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            var email = EmailTemplates.LowStockAlert(
                recipient,
                product.Name,
                product.Sku,
                notification.NewQuantityOnHand,
                product.MinStockLevel.Value);

            try
            {
                await _emailService.SendAsync(email, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send low stock alert to {Recipient} for product {Sku}", recipient, product.Sku);
            }
        }
    }
}
