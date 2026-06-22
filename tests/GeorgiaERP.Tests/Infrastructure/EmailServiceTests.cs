using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace GeorgiaERP.Tests.Infrastructure;

public class EmailServiceTests
{
    [Fact]
    public void PasswordReset_ReturnsValidEmailMessage()
    {
        var message = EmailTemplates.PasswordReset("user@test.ge", "abc123token", "testuser");

        message.To.Should().Be("user@test.ge");
        message.Subject.Should().Contain("Password Reset");
        message.HtmlBody.Should().Contain("abc123token");
        message.HtmlBody.Should().Contain("testuser");
        message.HtmlBody.Should().Contain("<html");
    }

    [Fact]
    public void LowStockAlert_ContainsProductNameAndSku()
    {
        var message = EmailTemplates.LowStockAlert("admin@test.ge", "Coca-Cola 0.5L", "SKU-001", 3, 10);

        message.To.Should().Be("admin@test.ge");
        message.Subject.Should().Contain("Low Stock");
        message.HtmlBody.Should().Contain("Coca-Cola 0.5L");
        message.HtmlBody.Should().Contain("SKU-001");
        message.HtmlBody.Should().Contain("3");
        message.HtmlBody.Should().Contain("10");
    }

    [Fact]
    public void OrderConfirmation_ContainsOrderNumber()
    {
        var message = EmailTemplates.OrderConfirmation("buyer@test.ge", "ORD-2024-001", 150.50m, "GEL",
            new DateTimeOffset(2024, 6, 15, 10, 0, 0, TimeSpan.FromHours(4)));

        message.To.Should().Be("buyer@test.ge");
        message.Subject.Should().Contain("ORD-2024-001");
        message.HtmlBody.Should().Contain("ORD-2024-001");
        message.HtmlBody.Should().Contain("150.50");
        message.HtmlBody.Should().Contain("GEL");
    }

    [Fact]
    public void WelcomeUser_ContainsUsername()
    {
        var message = EmailTemplates.WelcomeUser("new@test.ge", "nino.k", "TempPass123!");

        message.To.Should().Be("new@test.ge");
        message.Subject.Should().Contain("Welcome");
        message.HtmlBody.Should().Contain("nino.k");
        message.HtmlBody.Should().Contain("TempPass123!");
    }

    [Fact]
    public async Task SmtpEmailService_WithUnconfiguredSmtp_DoesNotThrow()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        var service = new SmtpEmailService(config, NullLogger<SmtpEmailService>.Instance);

        var message = new EmailMessage("test@test.ge", "Test Subject", "<p>Test</p>");

        var act = () => service.SendAsync(message);

        await act.Should().NotThrowAsync();
    }
}
