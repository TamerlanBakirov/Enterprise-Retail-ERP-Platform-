using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task InitializeAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var passwordService = scope.ServiceProvider.GetRequiredService<IPasswordService>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            await dbContext.Database.MigrateAsync();
            await SeedRolesAndPermissionsAsync(dbContext);
            await SeedAdminUserAsync(dbContext, passwordService, configuration, logger);
            logger.LogInformation("Database seeded successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error seeding database");
            throw;
        }
    }

    private static async Task SeedRolesAndPermissionsAsync(AppDbContext dbContext)
    {
        if (await dbContext.Roles.AnyAsync())
            return;

        var modules = new[] { "identity", "products", "inventory", "pos", "procurement", "compliance", "finance", "crm", "organization", "reports" };
        var actions = new[] { "read", "create", "update", "delete", "manage" };

        var permissions = new List<Permission>();
        foreach (var module in modules)
        {
            foreach (var action in actions)
            {
                var permission = Permission.Create(module, action, "*");
                permissions.Add(permission);
            }
        }

        dbContext.Permissions.AddRange(permissions);

        var roles = new[]
        {
            Role.Create("super_admin", "Super Administrator", "სუპერ ადმინისტრატორი", "Full system access", isSystem: true),
            Role.Create("company_admin", "Company Administrator", "კომპანიის ადმინისტრატორი", "Company-level administration", isSystem: true),
            Role.Create("store_manager", "Store Manager", "მაღაზიის მენეჯერი", "Store-level management", isSystem: true),
            Role.Create("cashier", "Cashier", "მოლარე", "POS operations", isSystem: true),
            Role.Create("warehouse_manager", "Warehouse Manager", "საწყობის მენეჯერი", "Warehouse operations", isSystem: true),
            Role.Create("accountant", "Accountant", "ბუღალტერი", "Finance and accounting", isSystem: true),
            Role.Create("procurement_officer", "Procurement Officer", "შესყიდვების ოფიცერი", "Procurement management", isSystem: true),
            Role.Create("inventory_clerk", "Inventory Clerk", "ინვენტარის კლერკი", "Inventory operations", isSystem: true),
            Role.Create("auditor", "Auditor", "აუდიტორი", "Read-only access for auditing", isSystem: true),
            Role.Create("viewer", "Viewer", "მნახველი", "Read-only access", isSystem: true),
        };

        dbContext.Roles.AddRange(roles);

        var superAdmin = roles[0];
        foreach (var permission in permissions)
        {
            var rp = RolePermission.Create(superAdmin.Id, permission.Id);
            dbContext.RolePermissions.Add(rp);
        }

        var cashierRole = roles[3];
        var cashierPermissions = permissions.Where(p =>
            p.Module is "pos" or "products" or "inventory" && p.Action is "read" or "create").ToList();
        foreach (var p in cashierPermissions)
        {
            dbContext.RolePermissions.Add(RolePermission.Create(cashierRole.Id, p.Id));
        }

        var viewerRole = roles[9];
        var viewerPermissions = permissions.Where(p => p.Action == "read").ToList();
        foreach (var p in viewerPermissions)
        {
            dbContext.RolePermissions.Add(RolePermission.Create(viewerRole.Id, p.Id));
        }

        await dbContext.SaveChangesAsync();
    }

    private static async Task SeedAdminUserAsync(
        AppDbContext dbContext,
        IPasswordService passwordService,
        IConfiguration configuration,
        ILogger logger)
    {
        if (await dbContext.Users.AnyAsync())
            return;

        // Admin password must be supplied via configuration/environment in non-dev
        // environments. Falls back to a well-known dev default only when unset.
        var adminPassword = configuration["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            adminPassword = "Admin@123!";
            logger.LogWarning(
                "Seed:AdminPassword not configured; seeding admin with the default development password. " +
                "Set Seed:AdminPassword (or the SEED__ADMINPASSWORD environment variable) before production use.");
        }

        var adminUser = User.Create(
            username: "admin",
            email: "admin@georgiaerp.local",
            passwordHash: passwordService.HashPassword(adminPassword),
            firstName: "System",
            lastName: "Administrator",
            firstNameKa: "სისტემის",
            lastNameKa: "ადმინისტრატორი",
            defaultLanguage: "ka");

        dbContext.Users.Add(adminUser);

        var superAdminRole = await dbContext.Roles.FirstAsync(r => r.Code == "super_admin");
        var userRole = UserRole.Create(adminUser.Id, superAdminRole.Id);
        dbContext.UserRoles.Add(userRole);

        await dbContext.SaveChangesAsync();
    }
}
