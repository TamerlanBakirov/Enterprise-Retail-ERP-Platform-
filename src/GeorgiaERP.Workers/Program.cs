using GeorgiaERP.Infrastructure;
using GeorgiaERP.Workers;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddSerilog(configuration => configuration
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());

// Shares the full data + RS.GE stack (DbContext, SOAP client, queue) with the API.
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHostedService<RsGeSubmissionConsumer>();
builder.Services.AddHostedService<RsGeRecoveryWorker>();
builder.Services.AddHostedService<InvoiceDeadlineMonitor>();

var host = builder.Build();
host.Run();
