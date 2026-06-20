using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace GeorgiaERP.Infrastructure.Persistence;

/// <summary>
/// Enables `dotnet ef migrations` / `dotnet ef database update` to instantiate
/// the DbContext at design time without spinning up the full application host.
/// The connection string here is only used for scaffolding metadata; the runtime
/// connection comes from configuration via DependencyInjection.AddInfrastructure.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("GEORGIA_ERP_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=georgia_erp;Username=erp_user;Password=erp_password";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(connectionString, npgsql =>
            npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
