using System.Text.Json;
using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Common;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Domain.Finance;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Licensing;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Warehouse;
using WarehouseEntity = GeorgiaERP.Domain.Organization.Warehouse;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly string _schema;
    private readonly IMediator? _mediator;

    public AppDbContext(DbContextOptions<AppDbContext> options, IMediator? mediator = null, string schema = "public")
        : base(options)
    {
        _mediator = mediator;
        _schema = schema;
    }

    // Organization
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<WarehouseEntity> Warehouses => Set<WarehouseEntity>();

    // Identity
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    // Products
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductBarcode> ProductBarcodes => Set<ProductBarcode>();
    public DbSet<ProductBundle> ProductBundles => Set<ProductBundle>();

    // Pricing
    public DbSet<PriceList> PriceLists => Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => Set<PriceListItem>();
    public DbSet<Promotion> Promotions => Set<Promotion>();

    // Inventory
    public DbSet<StockLevel> StockLevels => Set<StockLevel>();
    public DbSet<StockMovement> StockMovements => Set<StockMovement>();
    public DbSet<StockCount> StockCounts => Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => Set<StockCountLine>();
    public DbSet<TransferOrder> TransferOrders => Set<TransferOrder>();
    public DbSet<TransferOrderLine> TransferOrderLines => Set<TransferOrderLine>();

    // POS
    public DbSet<PosTerminal> PosTerminals => Set<PosTerminal>();
    public DbSet<PosSession> PosSessions => Set<PosSession>();
    public DbSet<PosTransaction> PosTransactions => Set<PosTransaction>();
    public DbSet<PosTransactionLine> PosTransactionLines => Set<PosTransactionLine>();
    public DbSet<PosPayment> PosPayments => Set<PosPayment>();
    public DbSet<DailyClosing> DailyClosings => Set<DailyClosing>();

    // Procurement
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<GoodsReceiptNote> GoodsReceiptNotes => Set<GoodsReceiptNote>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => Set<GoodsReceiptLine>();

    // Compliance
    public DbSet<FiscalDocument> FiscalDocuments => Set<FiscalDocument>();
    public DbSet<RsGeWaybill> RsGeWaybills => Set<RsGeWaybill>();
    public DbSet<RsGeCommunicationLog> RsGeCommunicationLogs => Set<RsGeCommunicationLog>();
    public DbSet<VatDeclaration> VatDeclarations => Set<VatDeclaration>();

    // Finance
    public DbSet<ChartOfAccount> ChartOfAccounts => Set<ChartOfAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();

    // CRM
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<LoyaltyTransaction> LoyaltyTransactions => Set<LoyaltyTransaction>();

    // Licensing
    public DbSet<License> Licenses => Set<License>();

    // Warehouse
    public DbSet<WarehouseLocation> WarehouseLocations => Set<WarehouseLocation>();
    public DbSet<ReceivingOrder> ReceivingOrders => Set<ReceivingOrder>();
    public DbSet<ReceivingOrderLine> ReceivingOrderLines => Set<ReceivingOrderLine>();
    public DbSet<ShippingOrder> ShippingOrders => Set<ShippingOrder>();
    public DbSet<ShippingOrderLine> ShippingOrderLines => Set<ShippingOrderLine>();

    // Audit
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.Ignore<DomainEvent>();
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Explicitly ensure AuditLog is part of the model
        modelBuilder.Entity<AuditLog>();

        // Apply global query filter for soft-deleted entities
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDeletable).IsAssignableFrom(entityType.ClrType))
            {
                var method = typeof(AppDbContext)
                    .GetMethod(nameof(ApplySoftDeleteFilter),
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
                    .MakeGenericMethod(entityType.ClrType);

                method.Invoke(null, [modelBuilder]);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    private static void ApplySoftDeleteFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class, ISoftDeletable
    {
        modelBuilder.Entity<TEntity>().HasQueryFilter(e => e.DeletedAt == null);
    }

    // Guards against re-entrancy: prevents audit collection during audit save.
    private bool _isWritingAuditLogs;

    // Entity types that are tracked for audit trail. Only critical business entities
    // are audited to avoid noise from high-volume tables (POS transactions, stock movements).
    private static readonly HashSet<string> AuditedEntityTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(Domain.Finance.JournalEntry), nameof(Domain.Finance.BankAccount), nameof(Domain.Finance.ChartOfAccount),
        nameof(Domain.Compliance.FiscalDocument), nameof(Domain.Compliance.RsGeWaybill),
        nameof(Domain.Identity.User), nameof(Domain.Identity.Role), nameof(Domain.Identity.UserRole),
        nameof(Domain.Products.Product), nameof(Domain.Products.Category),
        nameof(Domain.Pricing.PriceList), nameof(Domain.Pricing.Promotion),
        nameof(Domain.Procurement.PurchaseOrder), nameof(Domain.Procurement.GoodsReceiptNote),
        nameof(Domain.Inventory.TransferOrder), nameof(Domain.Inventory.StockCount)
    };

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;

        foreach (var entry in ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
                    break;

                case EntityState.Modified:
                    entry.Property(nameof(IAuditableEntity.UpdatedAt)).CurrentValue = now;
                    entry.Property(nameof(IAuditableEntity.CreatedAt)).IsModified = false;
                    break;
            }
        }

        // Capture audit entries before save (original values are still available).
        // Skip audit collection during audit save (re-entrancy guard).
        var auditEntries = _isWritingAuditLogs ? [] : CollectAuditEntries();

        // Collect domain events before saving so they survive the Clear below.
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = entitiesWithEvents
            .SelectMany(e => e.DomainEvents)
            .ToList();

        // Clear events before dispatching to prevent re-entrancy issues
        // if a handler triggers another SaveChangesAsync call.
        foreach (var entity in entitiesWithEvents)
            entity.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Persist audit log entries (uses a separate SaveChanges to avoid recursion).
        // Gracefully handles cases where the audit_logs table doesn't exist yet
        // (e.g., during EnsureCreated() schema bootstrap).
        if (auditEntries.Count > 0)
        {
            try
            {
                _isWritingAuditLogs = true;
                AuditLogs.AddRange(auditEntries);
                await base.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                // Detach audit entries so they don't block future saves.
                foreach (var entry in ChangeTracker.Entries<AuditLog>()
                             .Where(e => e.State == EntityState.Added)
                             .ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
            finally
            {
                _isWritingAuditLogs = false;
            }
        }

        // Dispatch domain events after successful persistence.
        // Events are dispatched via MediatR as INotification (the DomainEvent
        // base record implements INotification), enabling decoupled handlers.
        if (_mediator is not null)
        {
            foreach (var domainEvent in domainEvents)
                await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }

    private List<AuditLog> CollectAuditEntries()
    {
        var auditEntries = new List<AuditLog>();

        foreach (var entry in ChangeTracker.Entries<BaseEntity>())
        {
            if (entry.State == EntityState.Detached || entry.State == EntityState.Unchanged)
                continue;

            var entityType = entry.Entity.GetType().Name;
            if (!AuditedEntityTypes.Contains(entityType))
                continue;

            var entityId = entry.Entity.Id.ToString();
            var action = entry.State switch
            {
                EntityState.Added => "Created",
                EntityState.Modified => "Updated",
                EntityState.Deleted => "Deleted",
                _ => (string?)null
            };

            if (action is null)
                continue;

            // Get the user ID from IAuditableEntity if available
            Guid? userId = null;
            if (entry.Entity is IAuditableEntity auditable)
            {
                userId = entry.State == EntityState.Added ? auditable.CreatedBy : auditable.UpdatedBy;
                if (userId == Guid.Empty) userId = null;
            }

            string? changedProperties = null;
            if (entry.State == EntityState.Modified)
            {
                var changes = new Dictionary<string, object?>();
                foreach (var prop in entry.Properties.Where(p => p.IsModified))
                {
                    changes[prop.Metadata.Name] = new
                    {
                        Old = prop.OriginalValue?.ToString(),
                        New = prop.CurrentValue?.ToString()
                    };
                }
                if (changes.Count > 0)
                    changedProperties = JsonSerializer.Serialize(changes);
            }

            auditEntries.Add(AuditLog.Create(entityType, entityId, action, changedProperties, userId));
        }

        return auditEntries;
    }
}
