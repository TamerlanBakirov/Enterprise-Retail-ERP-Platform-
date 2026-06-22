using GeorgiaERP.Application.Common;
using FluentAssertions;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class NotificationServiceTests
{
    [Fact]
    public void NotificationEvents_HasExpectedConstants()
    {
        NotificationEvents.LowStockAlert.Should().Be("LowStockAlert");
        NotificationEvents.StockAdjusted.Should().Be("StockAdjusted");
        NotificationEvents.StockTransferUpdated.Should().Be("StockTransferUpdated");
        NotificationEvents.WaybillStatusChanged.Should().Be("WaybillStatusChanged");
        NotificationEvents.WaybillSubmissionFailed.Should().Be("WaybillSubmissionFailed");
        NotificationEvents.OrderPlaced.Should().Be("OrderPlaced");
        NotificationEvents.PurchaseOrderStatusChanged.Should().Be("PurchaseOrderStatusChanged");
        NotificationEvents.PosTransactionCompleted.Should().Be("PosTransactionCompleted");
        NotificationEvents.DailyClosingCompleted.Should().Be("DailyClosingCompleted");
        NotificationEvents.SystemAlert.Should().Be("SystemAlert");
    }

    [Fact]
    public void NotificationPayload_CanBeCreated()
    {
        var payload = new NotificationPayload
        {
            EventType = NotificationEvents.LowStockAlert,
            Title = "Test Alert",
            Message = "5 items below minimum",
            Severity = "warning",
            Data = new { Count = 5 }
        };

        payload.EventType.Should().Be(NotificationEvents.LowStockAlert);
        payload.Title.Should().Be("Test Alert");
        payload.Message.Should().Be("5 items below minimum");
        payload.Severity.Should().Be("warning");
        payload.Data.Should().NotBeNull();
        payload.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void NotificationPayload_DefaultTimestamp_IsNow()
    {
        var before = DateTimeOffset.UtcNow;
        var payload = new NotificationPayload
        {
            EventType = "test",
            Title = "test",
            Message = "test"
        };
        var after = DateTimeOffset.UtcNow;

        payload.Timestamp.Should().BeOnOrAfter(before);
        payload.Timestamp.Should().BeOnOrBefore(after);
    }
}
