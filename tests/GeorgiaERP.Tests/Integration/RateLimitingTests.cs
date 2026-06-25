using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Threading.RateLimiting;
using FluentAssertions;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace GeorgiaERP.Tests.Integration;

/// <summary>
/// Rate limiting tests use their own isolated factory so that the production
/// rate limit configuration is tested without interference from other tests.
/// </summary>
[Collection("Integration")]
public class RateLimitingTests : IAsyncLifetime
{
    private RateLimitTestFactory _factory = null!;

    public Task InitializeAsync()
    {
        _factory = new RateLimitTestFactory();
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task RateLimiting_Returns429_WhenAuthLimitExceeded()
    {
        var client = _factory.CreateClient();
        HttpStatusCode lastStatus = HttpStatusCode.OK;
        // Auth endpoint has limit of 10/min per IP - send 15 requests to trigger 429
        for (var i = 0; i < 15; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { username = "x", password = "x" });
            lastStatus = response.StatusCode;
            if (lastStatus == (HttpStatusCode)429) break;
        }
        lastStatus.Should().Be((HttpStatusCode)429);
    }

    [Fact]
    public async Task RateLimiting_Returns429Body_ContainsErrorCode()
    {
        var client = _factory.CreateClient();
        HttpResponseMessage? rateLimitedResponse = null;
        for (var i = 0; i < 15; i++)
        {
            var response = await client.PostAsJsonAsync("/api/v1/auth/login",
                new { username = "y", password = "y" });
            if (response.StatusCode == (HttpStatusCode)429)
            {
                rateLimitedResponse = response;
                break;
            }
        }
        rateLimitedResponse.Should().NotBeNull("at least one request should be rate limited");
        var body = await rateLimitedResponse!.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("errorCode").GetString().Should().Be("RATE_LIMITED");
    }

    [Fact]
    public async Task HealthEndpoint_IsNotRateLimited()
    {
        var client = _factory.CreateClient();
        // Health endpoints should not be rate limited even after many requests
        HttpStatusCode lastStatus = HttpStatusCode.OK;
        for (var i = 0; i < 110; i++)
        {
            var response = await client.GetAsync("/health");
            lastStatus = response.StatusCode;
        }
        // Even after 110 requests (exceeding the 100/min global limit), health should still respond OK
        lastStatus.Should().Be(HttpStatusCode.OK);
    }

    /// <summary>
    /// Minimal factory that uses the real rate limiting configuration.
    /// </summary>
    private sealed class RateLimitTestFactory : WebApplicationFactory<Program>
    {
        private const string TestJwtKey = "TEST-ONLY-JWT-SECRET-KEY-THAT-IS-AT-LEAST-SIXTY-FOUR-CHARACTERS-LONG-FOR-TESTS";
        private readonly SqliteConnection _connection = new("DataSource=:memory:");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fake_for_test",
                    ["ConnectionStrings:Redis"] = "",
                    ["RsGe:Queue:HostName"] = "localhost",
                    ["Jwt:SecretKey"] = TestJwtKey,
                    ["Licensing:SigningKey"] = "TEST-LICENSE-SIGNING-KEY-WITH-AT-LEAST-THIRTY-TWO-CHARS",
                    ["Seed:Demo"] = "false",
                    ["HealthChecksUI:Enabled"] = "false"
                });
            });

            builder.ConfigureServices(services =>
            {
                var efDescriptors = services
                    .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                             || d.ServiceType == typeof(DbContextOptions)
                             || d.ServiceType == typeof(AppDbContext)
                             || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                    .ToList();
                foreach (var d in efDescriptors) services.Remove(d);

                _connection.Open();

                var schemaOptions = new DbContextOptionsBuilder<AppDbContext>()
                    .UseSqlite(_connection)
                    .Options;
                using (var schemaDb = new AppDbContext(schemaOptions))
                {
                    schemaDb.Database.EnsureCreated();
                }

                services.AddDbContext<AppDbContext>(options =>
                {
                    options.UseSqlite(_connection);
                    options.ConfigureWarnings(w =>
                        w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
                });

                services.AddSingleton<SqliteConnection>(_connection);

                services.RemoveAll<IMachineIdProvider>();
                services.AddSingleton<IMachineIdProvider>(new TestMachineIdProvider());

                services.RemoveAll<IRabbitMqConnection>();
                services.AddSingleton<IRabbitMqConnection, NullRabbitMqConnection>();

                services.RemoveAll<IRsGeQueuePublisher>();
                services.AddScoped<IRsGeQueuePublisher, NullRsGeQueuePublisher>();

                services.RemoveAll<IRsGeSoapClient>();
                services.AddScoped<IRsGeSoapClient, NullRsGeSoapClient>();

                services.RemoveAll<IRsGeCommunicationLogger>();
                services.AddScoped<IRsGeCommunicationLogger, NullRsGeCommunicationLogger>();

                services.RemoveAll<IEmailService>();
                services.AddSingleton<IEmailService, NullEmailService>();

                services.RemoveAll<IRsGeSubmissionProcessor>();
                services.AddScoped<IRsGeSubmissionProcessor, NullRsGeSubmissionProcessor>();

                services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(TestJwtKey));
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = "GeorgiaERP",
                        ValidateAudience = true,
                        ValidAudience = "GeorgiaERP.Client",
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = key,
                        ValidateLifetime = true,
                        ClockSkew = TimeSpan.FromSeconds(30)
                    };
                });

                services.RemoveAll<GeorgiaERP.Application.Common.IJwtTokenService>();
                services.AddSingleton<GeorgiaERP.Application.Common.IJwtTokenService>(provider =>
                {
                    var config = new ConfigurationBuilder()
                        .AddInMemoryCollection(new Dictionary<string, string?>
                        {
                            ["Jwt:SecretKey"] = TestJwtKey,
                            ["Jwt:Issuer"] = "GeorgiaERP",
                            ["Jwt:Audience"] = "GeorgiaERP.Client"
                        })
                        .Build();
                    return new GeorgiaERP.Infrastructure.Identity.JwtTokenService(config);
                });

                // Do NOT override rate limiting here -- use production config for rate limit tests

                var healthDescriptors = services
                    .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true
                              || d.ImplementationType?.FullName?.Contains("HealthCheck") == true)
                    .ToList();
                foreach (var d in healthDescriptors) services.Remove(d);
                services.AddHealthChecks();
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
                _connection.Dispose();
        }

        private sealed class TestMachineIdProvider : IMachineIdProvider
        {
            public string GetMachineId() => "TEST-MACHINE-RATELIMIT";
        }

        private sealed class NullRabbitMqConnection : IRabbitMqConnection
        {
            public Task<RabbitMQ.Client.IConnection> GetConnectionAsync(CancellationToken cancellationToken = default)
                => throw new NotSupportedException("RabbitMQ is not available in tests");
            public Task EnsureTopologyAsync(CancellationToken cancellationToken = default)
                => Task.CompletedTask;
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }

        private sealed class NullRsGeQueuePublisher : IRsGeQueuePublisher
        {
            public Task PublishAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }

        private sealed class NullRsGeSoapClient : IRsGeSoapClient
        {
            public Task<string> GetMyIpAsync() => Task.FromResult("127.0.0.1");
            public Task<RsGeServiceUser> CheckServiceUserAsync(string su, string sp) => Task.FromResult(new RsGeServiceUser("TEST", "TEST"));
            public Task<RsGeNameResult> GetNameFromTinAsync(string tin) => Task.FromResult(new RsGeNameResult("Test", true));
            public Task<bool> IsVatPayerAsync(string tin) => Task.FromResult(true);
            public Task<IReadOnlyList<RsGeUnit>> GetUnitsAsync() => Task.FromResult<IReadOnlyList<RsGeUnit>>([]);
            public Task<IReadOnlyList<RsGeTransportType>> GetTransportTypesAsync() => Task.FromResult<IReadOnlyList<RsGeTransportType>>([]);
            public Task<IReadOnlyList<RsGeWaybillType>> GetWaybillTypesAsync() => Task.FromResult<IReadOnlyList<RsGeWaybillType>>([]);
            public Task<RsGeWaybillResult> SaveWaybillAsync(RsGeWaybillRequest r) => Task.FromResult(new RsGeWaybillResult(true, 1, "WB-001", null, null));
            public Task<RsGeResult> SendWaybillAsync(int id) => Task.FromResult(new RsGeResult(true, null, null));
            public Task<RsGeResult> ConfirmWaybillAsync(int id) => Task.FromResult(new RsGeResult(true, null, null));
            public Task<RsGeResult> CloseWaybillAsync(int id) => Task.FromResult(new RsGeResult(true, null, null));
            public Task<RsGeResult> RejectWaybillAsync(int id) => Task.FromResult(new RsGeResult(true, null, null));
            public Task<RsGeWaybillData?> GetWaybillAsync(int id) => Task.FromResult<RsGeWaybillData?>(null);
            public Task<RsGeResult> SaveInvoiceAsync(RsGeInvoiceRequest r) => Task.FromResult(new RsGeResult(true, null, null));
        }

        private sealed class NullRsGeCommunicationLogger : IRsGeCommunicationLogger
        {
            public Task LogRequestAsync(Guid? fiscalDocumentId, string operation, string endpoint, string requestPayload, Guid correlationId)
                => Task.CompletedTask;
            public Task LogResponseAsync(Guid correlationId, string responsePayload, int httpStatus, int durationMs, string? errorMessage)
                => Task.CompletedTask;
        }

        private sealed class NullRsGeSubmissionProcessor : IRsGeSubmissionProcessor
        {
            public Task<RsGeSubmissionResult> ProcessAsync(RsGeSubmissionMessage message, CancellationToken cancellationToken = default)
                => Task.FromResult(RsGeSubmissionResult.Success());
        }

        private sealed class NullEmailService : IEmailService
        {
            public Task SendAsync(EmailMessage message, CancellationToken cancellationToken = default)
                => Task.CompletedTask;
        }
    }
}
