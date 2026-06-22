using FluentAssertions;
using GeorgiaERP.Domain.Common;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class WebhookSubscriptionTests
{
    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var sub = WebhookSubscription.Create(
            "My Webhook", "https://example.com/hook", "secret123",
            ["order.created", "stock.low"]);

        sub.Name.Should().Be("My Webhook");
        sub.Url.Should().Be("https://example.com/hook");
        sub.Secret.Should().Be("secret123");
        sub.IsActive.Should().BeTrue();
        sub.MaxRetries.Should().Be(3);
        sub.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void Create_WithEmptyName_Throws()
    {
        var act = () => WebhookSubscription.Create("", "https://example.com", "secret", ["event"]);
        act.Should().Throw<ArgumentException>().WithMessage("*name*");
    }

    [Fact]
    public void Create_WithInvalidUrl_Throws()
    {
        var act = () => WebhookSubscription.Create("test", "not-a-url", "secret", ["event"]);
        act.Should().Throw<ArgumentException>().WithMessage("*URL*");
    }

    [Fact]
    public void Create_WithEmptySecret_Throws()
    {
        var act = () => WebhookSubscription.Create("test", "https://example.com", "", ["event"]);
        act.Should().Throw<ArgumentException>().WithMessage("*secret*");
    }

    [Fact]
    public void Create_WithNoEventTypes_Throws()
    {
        var act = () => WebhookSubscription.Create("test", "https://example.com", "secret", Array.Empty<string>());
        act.Should().Throw<ArgumentException>().WithMessage("*event type*");
    }

    [Fact]
    public void SubscribesTo_MatchingEvent_ReturnsTrue()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret",
            ["order.created", "stock.low"]);

        sub.SubscribesTo("order.created").Should().BeTrue();
        sub.SubscribesTo("stock.low").Should().BeTrue();
    }

    [Fact]
    public void SubscribesTo_NonMatchingEvent_ReturnsFalse()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret",
            ["order.created"]);

        sub.SubscribesTo("stock.low").Should().BeFalse();
    }

    [Fact]
    public void SubscribesTo_Wildcard_MatchesAll()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret", ["*"]);

        sub.SubscribesTo("order.created").Should().BeTrue();
        sub.SubscribesTo("stock.low").Should().BeTrue();
        sub.SubscribesTo("anything").Should().BeTrue();
    }

    [Fact]
    public void RecordDelivery_Success_ResetsFailures()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret", ["event"]);
        sub.RecordDelivery(false, "500");
        sub.RecordDelivery(false, "500");
        sub.ConsecutiveFailures.Should().Be(2);

        sub.RecordDelivery(true, "200 OK");
        sub.ConsecutiveFailures.Should().Be(0);
        sub.LastDeliveryStatus.Should().Be("200 OK");
    }

    [Fact]
    public void RecordDelivery_ManyFailures_AutoDisables()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret", ["event"], maxRetries: 2);
        // maxRetries * 3 = 6 consecutive failures to auto-disable
        for (int i = 0; i < 6; i++)
            sub.RecordDelivery(false, "500");

        sub.IsActive.Should().BeFalse();
    }

    [Fact]
    public void Update_ChangesFields()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret", ["event"]);

        sub.Update("new name", "https://new.com", ["event1", "event2"], 5);

        sub.Name.Should().Be("new name");
        sub.Url.Should().Be("https://new.com");
        sub.GetEventTypes().Should().Contain("event1");
        sub.GetEventTypes().Should().Contain("event2");
        sub.MaxRetries.Should().Be(5);
    }

    [Fact]
    public void GetEventTypes_ReturnsCorrectList()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret",
            ["order.created", "stock.low", "product.updated"]);

        var types = sub.GetEventTypes();
        types.Should().HaveCount(3);
        types.Should().Contain("order.created");
        types.Should().Contain("stock.low");
        types.Should().Contain("product.updated");
    }

    [Fact]
    public void Activate_Deactivate_TogglesState()
    {
        var sub = WebhookSubscription.Create("test", "https://example.com", "secret", ["event"]);
        sub.IsActive.Should().BeTrue();

        sub.Deactivate();
        sub.IsActive.Should().BeFalse();

        sub.Activate();
        sub.IsActive.Should().BeTrue();
    }
}

public class WebhookDeliveryLogTests
{
    [Fact]
    public void Create_WithSuccessfulDelivery_SetsProperties()
    {
        var subId = Guid.NewGuid();
        var log = WebhookDeliveryLog.Create(
            subId, "order.created", "{\"data\":\"test\"}", 1,
            true, 200, "OK", null, 150);

        log.SubscriptionId.Should().Be(subId);
        log.EventType.Should().Be("order.created");
        log.AttemptNumber.Should().Be(1);
        log.Success.Should().BeTrue();
        log.HttpStatusCode.Should().Be(200);
        log.DurationMs.Should().Be(150);
        log.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void Create_WithFailedDelivery_SetsError()
    {
        var log = WebhookDeliveryLog.Create(
            Guid.NewGuid(), "event", "{}", 2,
            false, 500, "Internal Server Error", "Timeout", 5000);

        log.Success.Should().BeFalse();
        log.HttpStatusCode.Should().Be(500);
        log.ErrorMessage.Should().Be("Timeout");
    }

    [Fact]
    public void Create_TruncatesLongResponseBody()
    {
        var longResponse = new string('x', 3000);
        var log = WebhookDeliveryLog.Create(
            Guid.NewGuid(), "event", "{}", 1, false, 500, longResponse);

        log.ResponseBody!.Length.Should().Be(2000);
    }
}
