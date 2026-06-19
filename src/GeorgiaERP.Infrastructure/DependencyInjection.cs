using System.Text;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Application.Compliance;
using GeorgiaERP.Application.Licensing;
using GeorgiaERP.Infrastructure.Identity;
using GeorgiaERP.Infrastructure.Licensing;
using GeorgiaERP.Infrastructure.Messaging;
using GeorgiaERP.Infrastructure.Persistence;
using GeorgiaERP.Infrastructure.RsGe;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using IAuthenticationService = GeorgiaERP.Application.Common.IAuthenticationService;
using IPasswordService = GeorgiaERP.Application.Common.IPasswordService;

namespace GeorgiaERP.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
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
        });

        services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

        services.AddSingleton<IPasswordService, PasswordService>();
        services.AddSingleton<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();

        services.AddHttpClient<IRsGeSoapClient, RsGeSoapClient>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(
                int.TryParse(configuration["RsGe:TimeoutSeconds"], out var timeout) ? timeout : 30);
            client.DefaultRequestHeaders.Add("Accept", "text/xml");
        });
        services.AddScoped<IRsGeCommunicationLogger, RsGeCommunicationLogger>();

        // RS.GE compliance queue + submission pipeline
        services.Configure<RsGeQueueOptions>(configuration.GetSection(RsGeQueueOptions.SectionName));
        services.AddSingleton<IRabbitMqConnection, RabbitMqConnection>();
        services.AddScoped<IRsGeQueuePublisher, RabbitMqQueuePublisher>();
        services.AddScoped<IRsGeSubmissionProcessor, RsGeSubmissionProcessor>();

        services.AddScoped<ILicenseValidator, LocalLicenseValidator>();
        services.AddSingleton<IMachineIdProvider, MachineIdProviderService>();

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
