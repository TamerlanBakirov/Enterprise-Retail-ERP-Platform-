using System.IO.Compression;
using System.Threading.RateLimiting;
using GeorgiaERP.Application;
using GeorgiaERP.Infrastructure;
using GeorgiaERP.Infrastructure.HealthChecks;
using GeorgiaERP.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi.Models;
using Prometheus;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithMachineName()
        .Enrich.WithEnvironmentName()
        .Enrich.WithProperty("Application", "GeorgiaERP.Api")
        .WriteTo.Console(outputTemplate:
            "[{Timestamp:HH:mm:ss} {Level:u3}] [{CorrelationId}] {Message:lj}{NewLine}{Exception}")
        .WriteTo.File(
            path: "logs/georgia-erp-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 14,
            outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{CorrelationId}] {SourceContext} {Message:lj}{NewLine}{Exception}"));

    builder.Services.AddApplication();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);

    builder.Services.AddControllers(options =>
        options.Filters.Add<GeorgiaERP.Api.Middleware.PermissionAuthorizationFilter>());
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "Georgia ERP Platform API",
            Version = "v1",
            Description = "Enterprise Retail ERP for Georgia with RS.GE fiscal integration."
        });

        var jwtScheme = new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the JWT token returned by /api/v1/auth/login.",
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
        };
        options.AddSecurityDefinition("Bearer", jwtScheme);
        options.AddSecurityRequirement(new OpenApiSecurityRequirement { [jwtScheme] = Array.Empty<string>() });
    });

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy
                .WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:3000"])
                .WithHeaders("Authorization", "Content-Type", "Accept", "Accept-Language", "X-Requested-With", "X-Correlation-ID")
                .WithMethods("GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS")
                .AllowCredentials()
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10));
        });
    });

    builder.Services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        options.AddPolicy("fixed", httpContext =>
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue("RateLimiting:FixedPermitLimit", 100);
            return RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1)
                });
        });

        options.AddPolicy("auth", httpContext =>
        {
            var config = httpContext.RequestServices.GetRequiredService<IConfiguration>();
            var limit = config.GetValue("RateLimiting:AuthPermitLimit", 10);
            return RateLimitPartition.GetFixedWindowLimiter(
                httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = limit,
                    Window = TimeSpan.FromMinutes(1)
                });
        });
    });

    // Response compression: Brotli (preferred) + Gzip for JSON/text payloads.
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
        options.Providers.Add<BrotliCompressionProvider>();
        options.Providers.Add<GzipCompressionProvider>();
        options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        [
            "application/json",
            "application/xml",
            "text/xml",
            "text/json"
        ]);
    });
    builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);
    builder.Services.Configure<GzipCompressionProviderOptions>(options =>
        options.Level = CompressionLevel.Fastest);

    // Response caching: enables cache-control header processing.
    builder.Services.AddResponseCaching();

    // ── Health Checks ─────────────────────────────────────────────────
    var healthChecks = builder.Services.AddHealthChecks()
        .AddDbContextCheck<AppDbContext>("database", tags: ["ready"]);

    // PostgreSQL direct connectivity check (complementary to EF check).
    var pgConnection = builder.Configuration.GetConnectionString("DefaultConnection");
    if (!string.IsNullOrWhiteSpace(pgConnection))
    {
        healthChecks.AddNpgSql(
            pgConnection,
            name: "postgresql",
            tags: ["ready", "db"],
            timeout: TimeSpan.FromSeconds(5));
    }

    // Redis health check.
    var redisConnection = builder.Configuration.GetConnectionString("Redis");
    if (!string.IsNullOrWhiteSpace(redisConnection))
    {
        healthChecks.AddRedis(
            redisConnection,
            name: "redis",
            tags: ["ready", "cache"],
            timeout: TimeSpan.FromSeconds(3));
    }

    // RabbitMQ health check: reuses the IRabbitMqConnection already registered
    // by the infrastructure layer. The custom RabbitMqQueueDepthHealthCheck
    // (registered via AddRsGeHealthChecks) provides deeper queue-level monitoring.
    // No separate AddRabbitMQ call needed since AddRsGeHealthChecks covers it.

    // RS.GE endpoint reachability check.
    var rsGeUrl = builder.Configuration["RsGe:WaybillServiceUrl"];
    if (!string.IsNullOrWhiteSpace(rsGeUrl))
    {
        healthChecks.AddUrlGroup(
            new Uri(rsGeUrl),
            name: "rsge-endpoint",
            tags: ["ready", "compliance"],
            timeout: TimeSpan.FromSeconds(10));
    }

    // RS.GE SOAP endpoint functional check and RabbitMQ queue depth monitoring.
    healthChecks.AddRsGeHealthChecks();

    // Health Check UI (dev/staging only).
    if (builder.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("HealthChecksUI:Enabled"))
    {
        builder.Services
            .AddHealthChecksUI(options =>
            {
                options.SetEvaluationTimeInSeconds(30);
                options.MaximumHistoryEntriesPerEndpoint(100);
                options.AddHealthCheckEndpoint("Georgia ERP API", "/health/ready");
            })
            .AddInMemoryStorage();
    }

    var app = builder.Build();

    // ── Middleware Pipeline ────────────────────────────────────────────
    // Correlation ID goes first so all downstream middleware and log entries include it.
    app.UseMiddleware<GeorgiaERP.Api.Middleware.CorrelationIdMiddleware>();
    // Prometheus metrics middleware captures request duration including all downstream processing.
    app.UseMiddleware<GeorgiaERP.Api.Middleware.PrometheusMetricsMiddleware>();

    app.UseMiddleware<GeorgiaERP.Api.Middleware.RequestSizeLimitMiddleware>();
    app.UseMiddleware<GeorgiaERP.Api.Middleware.SecurityHeadersMiddleware>();
    app.UseMiddleware<GeorgiaERP.Api.Middleware.ExceptionHandlingMiddleware>();
    app.UseMiddleware<GeorgiaERP.Api.Middleware.SecurityAuditMiddleware>();
    app.UseMiddleware<GeorgiaERP.Api.Middleware.RequestResponseLoggingMiddleware>();
    app.UseSerilogRequestLogging(options =>
    {
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("CorrelationId", httpContext.Items["CorrelationId"]?.ToString() ?? "none");
            diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown");
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers.UserAgent.ToString());
        };
        // Reduce noise from successful requests at Info; log errors at Error.
        options.GetLevel = (httpContext, elapsed, ex) =>
        {
            if (ex is not null || httpContext.Response.StatusCode >= 500)
                return LogEventLevel.Error;
            if (httpContext.Response.StatusCode >= 400)
                return LogEventLevel.Warning;
            return LogEventLevel.Information;
        };
    });

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    app.UseHttpsRedirection();

    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("Swagger:Enabled"))
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Georgia ERP API v1");
            options.DocumentTitle = "Georgia ERP API";
        });
    }

    app.UseCors("AllowFrontend");
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();

    // ── Prometheus Metrics Endpoint ───────────────────────────────────
    app.UseHttpMetrics(); // Built-in ASP.NET Core HTTP metrics from prometheus-net
    app.MapMetrics("/metrics"); // Exposes /metrics endpoint for Prometheus scraping

    app.MapControllers().RequireRateLimiting("fixed");

    // ── Health Check Endpoints ────────────────────────────────────────

    // Liveness: simple check that the process is running and can handle requests.
    // Kubernetes uses this to decide whether to restart the pod.
    app.MapHealthChecks("/health/live", new HealthCheckOptions
    {
        Predicate = _ => false, // No dependency checks; just confirms the process is alive.
        ResponseWriter = WriteMinimalHealthResponse
    });

    // Readiness: checks all dependencies (DB, Redis, RabbitMQ, RS.GE).
    // Kubernetes uses this to decide whether to route traffic to the pod.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready"),
        ResponseWriter = WriteDetailedHealthResponse
    });

    // Legacy /health endpoint for backward compatibility.
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = WriteDetailedHealthResponse
    });

    // Health Check UI (when enabled).
    if (app.Environment.IsDevelopment() || app.Configuration.GetValue<bool>("HealthChecksUI:Enabled"))
    {
        app.MapHealthChecksUI(options =>
        {
            options.UIPath = "/health-ui";
            options.ApiPath = "/health-ui-api";
        });
    }

    if (args.Contains("--seed") || app.Environment.IsDevelopment())
    {
        await SeedData.InitializeAsync(app.Services);
    }

    if (args.Contains("--seed-demo") || app.Configuration.GetValue<bool>("Seed:Demo"))
    {
        await SeedDemoData.InitializeAsync(app.Services);
    }

    Log.Information("Georgia ERP Platform starting up...");
    app.Run();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Writes a minimal health response (just status text) for the liveness probe.
/// </summary>
static Task WriteMinimalHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";
    var status = report.Status == HealthStatus.Healthy ? "healthy" : "unhealthy";
    return context.Response.WriteAsync($"{{\"status\":\"{status}\"}}");
}

/// <summary>
/// Writes a detailed health response with per-component status, duration,
/// and exception details for the readiness probe and admin diagnostics.
/// </summary>
static Task WriteDetailedHealthResponse(HttpContext context, HealthReport report)
{
    context.Response.ContentType = "application/json";

    var entries = report.Entries.Select(e => new
    {
        name = e.Key,
        status = e.Value.Status.ToString(),
        duration = e.Value.Duration.TotalMilliseconds.ToString("F1") + "ms",
        description = e.Value.Description,
        error = e.Value.Exception?.Message,
        tags = e.Value.Tags
    });

    var result = new
    {
        status = report.Status.ToString(),
        totalDuration = report.TotalDuration.TotalMilliseconds.ToString("F1") + "ms",
        entries
    };

    return context.Response.WriteAsJsonAsync(result);
}

public partial class Program { }
