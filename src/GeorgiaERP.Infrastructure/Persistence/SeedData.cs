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
            if (!dbContext.Database.ProviderName?.Contains("Sqlite", StringComparison.OrdinalIgnoreCase) ?? true)
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
        {
            await ReconcileSystemRolePermissionsAsync(dbContext);
            return;
        }

        var modules = new[] { "identity", "products", "inventory", "pos", "procurement", "compliance", "finance", "crm", "organization", "reports", "warehouse" };
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
            (p.Module is "pos" or "products" or "inventory") && p.Action is "read" or "create").ToList();
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
        await ReconcileSystemRolePermissionsAsync(dbContext);
    }

    private static async Task ReconcileSystemRolePermissionsAsync(AppDbContext dbContext)
    {
        var roles = await dbContext.Roles.Where(r => r.IsSystem).ToListAsync();
        var permissions = await dbContext.Permissions.ToListAsync();
        var roleIds = roles.Select(r => r.Id).ToList();
        var existing = await dbContext.RolePermissions.Where(rp => roleIds.Contains(rp.RoleId)).ToListAsync();

        foreach (var role in roles)
        {
            var desiredIds = permissions
                .Where(permission => IsPermissionAllowed(role.Code, permission.Module, permission.Action))
                .Select(permission => permission.Id)
                .ToHashSet();
            var current = existing.Where(rp => rp.RoleId == role.Id).ToList();

            dbContext.RolePermissions.RemoveRange(current.Where(rp => !desiredIds.Contains(rp.PermissionId)));
            var currentIds = current.Select(rp => rp.PermissionId).ToHashSet();
            dbContext.RolePermissions.AddRange(desiredIds
                .Where(id => !currentIds.Contains(id))
                .Select(id => RolePermission.Create(role.Id, id)));
        }

        await dbContext.SaveChangesAsync();
    }

    private static bool IsPermissionAllowed(string role, string module, string action) => role switch
    {
        "super_admin" or "company_admin" => true,
        "store_manager" => module is "products" or "inventory" or "pos" or "crm" or "organization" or "reports" or "warehouse",
        "cashier" => (module == "pos" && action is "read" or "create" or "manage") ||
                     (module is "products" or "inventory" && action == "read") ||
                     (module == "crm" && action is "read" or "create"),
        "warehouse_manager" => module is "products" or "inventory" or "warehouse" && action is "read" or "create" or "update" or "manage",
        "accountant" => module is "finance" or "compliance" or "reports" && action is not "delete",
        "procurement_officer" => module is "procurement" or "products" or "inventory" or "warehouse" && action is not "delete",
        "inventory_clerk" => module is "inventory" or "products" or "warehouse" && action is "read" or "create" or "update",
        "auditor" or "viewer" => action == "read",
        _ => false
    };

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
