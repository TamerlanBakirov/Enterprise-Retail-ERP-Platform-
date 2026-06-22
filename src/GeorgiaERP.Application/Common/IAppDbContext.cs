using GeorgiaERP.Domain.Organization;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Domain.Products;
using GeorgiaERP.Domain.Pricing;
using GeorgiaERP.Domain.Inventory;
using GeorgiaERP.Domain.POS;
using GeorgiaERP.Domain.Procurement;
using GeorgiaERP.Domain.Compliance;
using GeorgiaERP.Domain.Finance;
using GeorgiaERP.Domain.CRM;
using GeorgiaERP.Domain.Licensing;
using GeorgiaERP.Domain.Warehouse;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Common;

public interface IAppDbContext
{
    DbSet<Company> Companies { get; }
    DbSet<Store> Stores { get; }
    DbSet<Domain.Organization.Warehouse> Warehouses { get; }
    DbSet<User> Users { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserRole> UserRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<Category> Categories { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductVariant> ProductVariants { get; }
    DbSet<ProductBarcode> ProductBarcodes { get; }
    DbSet<ProductBundle> ProductBundles { get; }
    DbSet<PriceList> PriceLists { get; }
    DbSet<PriceListItem> PriceListItems { get; }
    DbSet<Promotion> Promotions { get; }
    DbSet<StockLevel> StockLevels { get; }
    DbSet<StockMovement> StockMovements { get; }
    DbSet<StockCount> StockCounts { get; }
    DbSet<StockCountLine> StockCountLines { get; }
    DbSet<TransferOrder> TransferOrders { get; }
    DbSet<TransferOrderLine> TransferOrderLines { get; }
    DbSet<PosTerminal> PosTerminals { get; }
    DbSet<PosSession> PosSessions { get; }
    DbSet<PosTransaction> PosTransactions { get; }
    DbSet<PosTransactionLine> PosTransactionLines { get; }
    DbSet<PosPayment> PosPayments { get; }
    DbSet<DailyClosing> DailyClosings { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderLine> PurchaseOrderLines { get; }
    DbSet<GoodsReceiptNote> GoodsReceiptNotes { get; }
    DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
    DbSet<FiscalDocument> FiscalDocuments { get; }
    DbSet<RsGeWaybill> RsGeWaybills { get; }
    DbSet<RsGeCommunicationLog> RsGeCommunicationLogs { get; }
    DbSet<VatDeclaration> VatDeclarations { get; }
    DbSet<ChartOfAccount> ChartOfAccounts { get; }
    DbSet<JournalEntry> JournalEntries { get; }
    DbSet<JournalEntryLine> JournalEntryLines { get; }
    DbSet<BankAccount> BankAccounts { get; }
    DbSet<Customer> Customers { get; }
    DbSet<LoyaltyTransaction> LoyaltyTransactions { get; }
    DbSet<License> Licenses { get; }

    // Warehouse
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<ReceivingOrder> ReceivingOrders { get; }
    DbSet<ReceivingOrderLine> ReceivingOrderLines { get; }
    DbSet<ShippingOrder> ShippingOrders { get; }
    DbSet<ShippingOrderLine> ShippingOrderLines { get; }

    // Files
    DbSet<Domain.Common.FileMetadata> FileMetadata { get; }

    // Webhooks
    DbSet<Domain.Common.WebhookSubscription> WebhookSubscriptions { get; }
    DbSet<Domain.Common.WebhookDeliveryLog> WebhookDeliveryLogs { get; }

    // Audit
    DbSet<Domain.Common.AuditLog> AuditLogs { get; }

    // Backup
    DbSet<Domain.Common.BackupRecord> BackupRecords { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
