using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Infrastructure.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace GeorgiaERP.Tests.Application;

public class EmailServiceTests
{
    private static SmtpEmailService CreateService(SmtpEmailOptions? options = null)
    {
        var opts = Options.Create(options ?? new SmtpEmailOptions { Enabled = false });
        var logger = NullLoggerFactory.Instance.CreateLogger<SmtpEmailService>();
        return new SmtpEmailService(opts, logger);
    }

    [Fact]
    public async Task SendAsync_When_Disabled_Does_Not_Throw()
    {
        using var service = CreateService(new SmtpEmailOptions { Enabled = false });

        var message = new EmailMessage
        {
            To = "test@test.ge",
            Subject = "Test",
            HtmlBody = "<p>Test</p>"
        };

        // Should not throw even though SMTP is not configured
        var act = () => service.SendAsync(message);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task SendBatchAsync_When_Disabled_Does_Not_Throw()
    {
        using var service = CreateService(new SmtpEmailOptions { Enabled = false });

        var messages = new[]
        {
            new EmailMessage { To = "a@test.ge", Subject = "Test 1", HtmlBody = "<p>1</p>" },
            new EmailMessage { To = "b@test.ge", Subject = "Test 2", HtmlBody = "<p>2</p>" }
        };

        var act = () => service.SendBatchAsync(messages);
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public void SmtpEmailOptions_Has_Sensible_Defaults()
    {
        var options = new SmtpEmailOptions();

        options.Enabled.Should().BeFalse();
        options.Host.Should().Be("localhost");
        options.Port.Should().Be(587);
        options.UseSsl.Should().BeTrue();
        options.FromAddress.Should().Be("noreply@georgiaerp.ge");
        options.FromName.Should().Be("Georgia ERP Platform");
        options.TimeoutMs.Should().Be(30000);
    }

    [Fact]
    public void SmtpEmailOptions_SectionName_Is_Correct()
    {
        SmtpEmailOptions.SectionName.Should().Be("Email:Smtp");
    }
}
