using GeorgiaERP.Application.Common;
using GeorgiaERP.Domain.Common;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Domain.Finance;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Infrastructure.Persistence;

public class AppDbContext : DbContext, IAppDbContext
{
    private readonly string _schema;

    public AppDbContext(DbContextOptions<AppDbContext> options, string schema = "public")
        : base(options)
    {
        _schema = schema;
    }

    // Organization
    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();

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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(_schema);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

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

        return await base.SaveChangesAsync(cancellationToken);
    }
}
