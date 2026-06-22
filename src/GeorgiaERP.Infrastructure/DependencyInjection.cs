using System.Text;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Infrastructure.Caching;
using GeorgiaERP.Infrastructure.HealthChecks;
using GeorgiaERP.Infrastructure.Identity;
using GeorgiaERP.Infrastructure.Licensing;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.Reporting;
using GeorgiaERP.Infrastructure.RsGe;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Polly;
using IAuthenticationService = GeorgiaERP.Application.Common.IAuthenticationService;
using IJwtTokenService = GeorgiaERP.Application.Common.IJwtTokenService;
using IPasswordService = GeorgiaERP.Application.Common.IPasswordService;
using IRsGeCommunicationLogger = GeorgiaERP.Application.Compliance.IRsGeCommunicationLogger;
using IRsGeSoapClient = GeorgiaERP.Application.Compliance.IRsGeSoapClient;

namespace GeorgiaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IAuditContextAccessor, AuditContextAccessor>();
        services.AddScoped<AuditSaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((provider, options) =>
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

            options.UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName);
                npgsqlOptions.EnableRetryOnFailure(
                    maxRetryCount: 3,
                    maxRetryDelay: TimeSpan.FromSeconds(10),
                    errorCodesToAdd: null);
            });

            options.AddInterceptors(provider.GetRequiredService<AuditSaveChangesInterceptor>());
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        // Redis distributed cache
        var redisConnection = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrWhiteSpace(redisConnection))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnection;
                options.InstanceName = "GeorgiaERP:";
            });
        }
        else
        {
            // Fallback to in-memory distributed cache for dev/test environments without Redis
            services.AddDistributedMemoryCache();
        }

        services.AddSingleton<ICacheService, RedisCacheService>();

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<ITotpVerifier, TotpVerifier>();
        services.AddSingleton<ITotpSecretProtector, AesTotpSecretProtector>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        // Register the concrete SOAP client under its own type so the decorator can resolve it.
        services.AddHttpClient<RsGeSoapClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                int.TryParse(configuration["RsGe:TimeoutSeconds"], out var timeout) ? timeout : 30);
            client.DefaultRequestHeaders.Add("Accept", "text/xml");
        });

        // Polly resilience pipeline: retry + circuit breaker + timeout for RS.GE SOAP calls.
        services.Configure<RsGeSoapClientResilienceOptions>(
            configuration.GetSection(RsGeSoapClientResilienceOptions.SectionName));
        services.AddSingleton<ResiliencePipeline>(provider =>
        {
            var resilienceOptions = new RsGeSoapClientResilienceOptions();
            configuration.GetSection(RsGeSoapClientResilienceOptions.SectionName).Bind(resilienceOptions);
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            return RsGeResiliencePipelineFactory.Create(resilienceOptions, loggerFactory);
        });

        // Decorator chain: RsGeSoapClient -> ResilientRsGeSoapClient -> CachedRsGeSoapClient
        // Inner: raw SOAP client. Middle: adds Polly resilience. Outer: adds Redis caching.
        services.AddScoped<IRsGeSoapClient>(provider =>
        {
            var innerClient = provider.GetRequiredService<RsGeSoapClient>();
            var pipeline = provider.GetRequiredService<ResiliencePipeline>();
            var resilientLogger = provider.GetRequiredService<ILoggerFactory>()
                .CreateLogger<ResilientRsGeSoapClient>();
            var resilientClient = new ResilientRsGeSoapClient(innerClient, pipeline, resilientLogger);
            return new CachedRsGeSoapClient(resilientClient, provider.GetRequiredService<ICacheService>());
        });
        services.AddScoped<IRsGeCommunicationLogger, RsGeCommunicationLogger>();

        // RS.GE compliance queue + submission pipeline
        services.Configure<RsGeQueueOptions>(configuration.GetSection(RsGeQueueOptions.SectionName));
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddSingleton<IMessageDeduplicator, RedisMessageDeduplicator>();
        services.AddScoped<IRsGeQueuePublisher, RabbitMqQueuePublisher>();
        services.AddScoped<IRsGeSubmissionProcessor, RsGeSubmissionProcessor>();

        services.AddScoped<ILicenseValidator, LocalLicenseValidator>();
        services.AddSingleton<ILicenseKeyValidator, HmacLicenseKeyValidator>();
        services.AddSingleton<IMachineIdProvider, MachineIdProviderService>();

        services.AddSingleton<IPdfGenerationService, PdfGenerationService>();

        return services;
    }

    /// <summary>
    /// Web-only JWT bearer authentication + authorization. Kept separate from
    /// <see cref="AddInfrastructure"/> so non-web hosts (the Workers service)
    /// can share the data/RS.GE stack without pulling in ASP.NET Core routing
    /// services that authorization depends on.
    /// </summary>
    public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var jwtSecretKey = configuration["Jwt:SecretKey"]
            ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
        if (jwtSecretKey.Length < 64 || jwtSecretKey.Contains("${", StringComparison.Ordinal))
            throw new InvalidOperationException("Jwt:SecretKey must be a resolved secret containing at least 64 characters for HMAC-SHA256.");
        if (jwtSecretKey.StartsWith("CHANGE-THIS", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Jwt:SecretKey is still set to the placeholder value. Generate a cryptographically random key.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = configuration["Jwt:Issuer"] ?? "GeorgiaERP",
                ValidateAudience = true,
                ValidAudience = configuration["Jwt:Audience"] ?? "GeorgiaERP.Client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(30)
            };
        });

        services.AddAuthorization();

        return services;
    }
}
