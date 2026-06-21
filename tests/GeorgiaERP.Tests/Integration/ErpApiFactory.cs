using System.Text;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;

namespace GeorgiaERP.Tests.Integration;

public class ErpApiFactory : WebApplicationFactory<Program>
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
                ["Jwt:SecretKey"] = "TEST-ONLY-JWT-SECRET-KEY-THAT-IS-AT-LEAST-SIXTY-FOUR-CHARACTERS-LONG-FOR-TESTS",
                ["Licensing:SigningKey"] = "TEST-LICENSE-SIGNING-KEY-WITH-AT-LEAST-THIRTY-TWO-CHARS",
                ["Seed:Demo"] = "false",
                ["HealthChecksUI:Enabled"] = "false",
                ["RateLimiting:FixedPermitLimit"] = "10000",
                ["RateLimiting:AuthPermitLimit"] = "10000"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace EF Core with SQLite in-memory (supports relational features)
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            _connection.Open();

            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseSqlite(_connection);
                options.ConfigureWarnings(w =>
                    w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            });

            // Ensure schema created
            services.AddSingleton<SqliteConnection>(_connection);

            // Stub out external infrastructure
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

            services.RemoveAll<IRsGeSubmissionProcessor>();
            services.AddScoped<IRsGeSubmissionProcessor, NullRsGeSubmissionProcessor>();

            // Re-configure JWT with test key (config overrides apply too late for AddJwtAuthentication)
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

            // Replace JwtTokenService so it signs with the test key
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

            // Replace health checks with basic (no external probes in tests)
            var healthDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true
                          || d.ImplementationType?.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var d in healthDescriptors) services.Remove(d);
            services.AddHealthChecks();
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);

        using var scope = host.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureDeleted();
        db.Database.EnsureCreated();

        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
            _connection.Dispose();
    }

    private sealed class TestMachineIdProvider : IMachineIdProvider
    {
        public string GetMachineId() => "TEST-MACHINE-001";
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
}
