using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace GeorgiaERP.Tests.Integration;

public class ErpApiFactory : WebApplicationFactory<Program>
{
    private readonly string _dbName = $"erp_test_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=fake_for_test",
                ["RsGe:Queue:HostName"] = "localhost"
            });
        });

        builder.ConfigureServices(services =>
        {
            // Replace EF Core with InMemory — remove ALL EF-related descriptors
            var efDescriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || d.ServiceType == typeof(AppDbContext)
                         || d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true)
                .ToList();
            foreach (var d in efDescriptors) services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseInMemoryDatabase(_dbName));

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

            // Replace health checks with basic (no DB probe on InMemory)
            var healthDescriptors = services
                .Where(d => d.ServiceType.FullName?.Contains("HealthCheck") == true)
                .ToList();
            foreach (var d in healthDescriptors) services.Remove(d);
            services.AddHealthChecks();
        });
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
