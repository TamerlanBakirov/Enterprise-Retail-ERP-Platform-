using FluentAssertions;
using GeorgiaERP.Infrastructure.Webhooks;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class WebhookDeliveryServiceTests
{
    [Fact]
    public void ComputeHmacSignature_ProducesConsistentHash()
    {
        var payload = "{\"event\":\"test\",\"data\":{}}";
        var secret = "my-webhook-secret";

        var sig1 = WebhookDeliveryService.ComputeHmacSignature(payload, secret);
        var sig2 = WebhookDeliveryService.ComputeHmacSignature(payload, secret);

        sig1.Should().NotBeNullOrEmpty();
        sig1.Should().Be(sig2);
    }

    [Fact]
    public void ComputeHmacSignature_DifferentSecrets_ProduceDifferentSignatures()
    {
        var payload = "{\"event\":\"test\"}";

        var sig1 = WebhookDeliveryService.ComputeHmacSignature(payload, "secret1");
        var sig2 = WebhookDeliveryService.ComputeHmacSignature(payload, "secret2");

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeHmacSignature_DifferentPayloads_ProduceDifferentSignatures()
    {
        var secret = "my-secret";

        var sig1 = WebhookDeliveryService.ComputeHmacSignature("{\"a\":1}", secret);
        var sig2 = WebhookDeliveryService.ComputeHmacSignature("{\"a\":2}", secret);

        sig1.Should().NotBe(sig2);
    }

    [Fact]
    public void ComputeHmacSignature_IsHexEncoded()
    {
        var sig = WebhookDeliveryService.ComputeHmacSignature("test", "secret");

        sig.Should().MatchRegex("^[0-9a-f]{64}$"); // SHA-256 = 64 hex chars
    }
}
