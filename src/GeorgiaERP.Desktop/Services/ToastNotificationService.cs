using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;

namespace GeorgiaERP.Desktop.Services;

/// <summary>
/// Manages toast notifications displayed in the WPF Desktop UI.
/// Listens to SignalR events and translates them into user-visible toast messages.
/// </summary>
public interface IToastNotificationService
{
    /// <summary>
    /// Observable collection of active toast messages for binding in the UI.
    /// </summary>
    ObservableCollection<ToastMessage> ActiveToasts { get; }

    /// <summary>
    /// Show a toast notification manually.
    /// </summary>
    void ShowToast(string title, string message, ToastSeverity severity = ToastSeverity.Info);

    /// <summary>
    /// Dismiss a specific toast.
    /// </summary>
    void DismissToast(ToastMessage toast);

    /// <summary>
    /// Start listening to SignalR notifications and displaying toasts.
    /// </summary>
    void StartListening();

    /// <summary>
    /// Stop listening to SignalR notifications.
    /// </summary>
    void StopListening();
}

public enum ToastSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

public sealed class ToastMessage
{
    public string Id { get; } = Guid.NewGuid().ToString();
    public required string Title { get; init; }
    public required string Message { get; init; }
    public ToastSeverity Severity { get; init; } = ToastSeverity.Info;
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.Now;
    public string? EventType { get; init; }
}

public sealed class ToastNotificationService : IToastNotificationService
{
    private readonly ISignalRNotificationService _signalR;
    private readonly Dispatcher _dispatcher;
    private const int MaxVisibleToasts = 5;
    private const int AutoDismissSeconds = 8;

    public ObservableCollection<ToastMessage> ActiveToasts { get; } = new();

    public ToastNotificationService(ISignalRNotificationService signalR)
    {
        _signalR = signalR;
        _dispatcher = Application.Current.Dispatcher;
    }

    public void StartListening()
    {
        _signalR.NotificationReceived += OnNotificationReceived;
    }

    public void StopListening()
    {
        _signalR.NotificationReceived -= OnNotificationReceived;
    }

    public void ShowToast(string title, string message, ToastSeverity severity = ToastSeverity.Info)
    {
        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Severity = severity,
        };

        AddToast(toast);
    }

    public void DismissToast(ToastMessage toast)
    {
        _dispatcher.Invoke(() => ActiveToasts.Remove(toast));
    }

    private void OnNotificationReceived(string eventType, JsonElement payload)
    {
        var title = TryGetString(payload, "Title") ?? TranslateEventType(eventType);
        var message = TryGetString(payload, "Message") ?? "New notification received.";
        var severityStr = TryGetString(payload, "Severity");
        var severity = ParseSeverity(severityStr);

        var toast = new ToastMessage
        {
            Title = title,
            Message = message,
            Severity = severity,
            EventType = eventType
        };

        AddToast(toast);
    }

    private void AddToast(ToastMessage toast)
    {
        _dispatcher.Invoke(() =>
        {
            // Limit visible toasts
            while (ActiveToasts.Count >= MaxVisibleToasts)
                ActiveToasts.RemoveAt(0);

            ActiveToasts.Add(toast);
        });

        // Auto-dismiss after delay (critical messages stay longer)
        var delay = toast.Severity == ToastSeverity.Critical
            ? TimeSpan.FromSeconds(AutoDismissSeconds * 2)
            : TimeSpan.FromSeconds(AutoDismissSeconds);

        ScheduleAutoDismiss(toast, delay);
    }

    private void ScheduleAutoDismiss(ToastMessage toast, TimeSpan delay)
    {
        var timer = new DispatcherTimer(DispatcherPriority.Normal, _dispatcher)
        {
            Interval = delay
        };
        timer.Tick += (_, _) =>
        {
            timer.Stop();
            DismissToast(toast);
        };
        timer.Start();
    }

    private static string TranslateEventType(string eventType)
    {
        return eventType switch
        {
            "LowStockAlert" => "Low Stock Alert",
            "StockAdjusted" => "Stock Adjusted",
            "StockTransferUpdated" => "Stock Transfer Updated",
            "WaybillStatusChanged" => "Waybill Status Changed",
            "WaybillSubmissionFailed" => "Waybill Submission Failed",
            "OrderPlaced" => "New Order",
            "PurchaseOrderStatusChanged" => "Purchase Order Updated",
            "PosTransactionCompleted" => "Transaction Completed",
            "DailyClosingCompleted" => "Daily Closing Completed",
            "SystemAlert" => "System Alert",
            _ => eventType
        };
    }

    private static ToastSeverity ParseSeverity(string? severity)
    {
        return severity?.ToLowerInvariant() switch
        {
            "warning" => ToastSeverity.Warning,
            "error" => ToastSeverity.Error,
            "critical" => ToastSeverity.Critical,
            _ => ToastSeverity.Info
        };
    }

    private static string? TryGetString(JsonElement element, string propertyName)
    {
        if (element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(propertyName, out var prop)
            && prop.ValueKind == JsonValueKind.String)
        {
            return prop.GetString();
        }
        return null;
    }
}
