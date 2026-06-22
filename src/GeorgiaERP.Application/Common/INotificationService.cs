namespace GeorgiaERP.Application.Common;

/// <summary>
/// Abstraction for pushing real-time notifications to connected clients.
/// Implementations use SignalR or similar transports.
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a notification to all connected clients.
    /// </summary>
    Task SendToAllAsync(string eventType, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a specific user by their user ID.
    /// </summary>
    Task SendToUserAsync(Guid userId, string eventType, object payload, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sends a notification to a group (e.g., "warehouse-{id}", "role-admin").
    /// </summary>
    Task SendToGroupAsync(string group, string eventType, object payload, CancellationToken cancellationToken = default);
}

/// <summary>
/// Well-known notification event types used throughout the ERP platform.
/// </summary>
public static class NotificationEvents
{
    // Inventory
    public const string LowStockAlert = "LowStockAlert";
    public const string StockAdjusted = "StockAdjusted";
    public const string StockTransferUpdated = "StockTransferUpdated";

    // Waybill / Compliance
    public const string WaybillStatusChanged = "WaybillStatusChanged";
    public const string WaybillSubmissionFailed = "WaybillSubmissionFailed";

    // Orders
    public const string OrderPlaced = "OrderPlaced";
    public const string PurchaseOrderStatusChanged = "PurchaseOrderStatusChanged";

    // POS
    public const string PosTransactionCompleted = "PosTransactionCompleted";
    public const string DailyClosingCompleted = "DailyClosingCompleted";

    // System
    public const string SystemAlert = "SystemAlert";
}

/// <summary>
/// Represents a real-time notification payload sent to clients.
/// </summary>
public sealed record NotificationPayload
{
    public required string EventType { get; init; }
    public required string Title { get; init; }
    public required string Message { get; init; }
    public string? Severity { get; init; } // info, warning, error, critical
    public object? Data { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
