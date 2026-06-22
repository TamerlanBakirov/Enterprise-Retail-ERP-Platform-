using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GeorgiaERP.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBackupRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_warehouses_LinkedStoreId",
                schema: "public",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.RenameIndex(
                name: "IX_vat_declarations_PeriodStart_PeriodEnd",
                schema: "public",
                table: "vat_declarations",
                newName: "IX_vat_declarations_period");

            migrationBuilder.RenameIndex(
                name: "IX_transfer_order_lines_TransferOrderId",
                schema: "public",
                table: "transfer_order_lines",
                newName: "IX_transfer_order_lines_transfer_order");

            migrationBuilder.RenameIndex(
                name: "IX_stock_movements_ProductId_WarehouseId",
                schema: "public",
                table: "stock_movements",
                newName: "IX_stock_movements_product_warehouse");

            migrationBuilder.RenameIndex(
                name: "IX_stock_levels_ProductId_VariantId_WarehouseId",
                schema: "public",
                table: "stock_levels",
                newName: "IX_stock_levels_product_variant_warehouse");

            migrationBuilder.RenameIndex(
                name: "IX_stock_count_lines_StockCountId",
                schema: "public",
                table: "stock_count_lines",
                newName: "IX_stock_count_lines_stock_count");

            migrationBuilder.RenameIndex(
                name: "IX_rsge_communication_logs_FiscalDocumentId",
                schema: "public",
                table: "rsge_communication_logs",
                newName: "IX_rsge_comm_logs_fiscal_document");

            migrationBuilder.RenameIndex(
                name: "IX_rsge_communication_logs_CorrelationId",
                schema: "public",
                table: "rsge_communication_logs",
                newName: "IX_rsge_comm_logs_correlation");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserId",
                schema: "public",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_user");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_orders_SupplierId",
                schema: "public",
                table: "purchase_orders",
                newName: "IX_purchase_orders_supplier");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_order_lines_PurchaseOrderId",
                schema: "public",
                table: "purchase_order_lines",
                newName: "IX_purchase_order_lines_purchase_order");

            migrationBuilder.RenameIndex(
                name: "IX_products_CategoryId",
                schema: "public",
                table: "products",
                newName: "IX_products_category");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_ProductId",
                schema: "public",
                table: "product_variants",
                newName: "IX_product_variants_product");

            migrationBuilder.RenameIndex(
                name: "IX_product_barcodes_ProductId",
                schema: "public",
                table: "product_barcodes",
                newName: "IX_product_barcodes_product");

            migrationBuilder.RenameIndex(
                name: "IX_price_list_items_PriceListId_ProductId_VariantId",
                schema: "public",
                table: "price_list_items",
                newName: "IX_price_list_items_list_product_variant");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transactions_StoreId_CreatedAt",
                schema: "public",
                table: "pos_transactions",
                newName: "IX_pos_transactions_store_date");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transactions_SessionId",
                schema: "public",
                table: "pos_transactions",
                newName: "IX_pos_transactions_session");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transaction_lines_TransactionId",
                schema: "public",
                table: "pos_transaction_lines",
                newName: "IX_pos_transaction_lines_transaction");

            migrationBuilder.RenameIndex(
                name: "IX_pos_sessions_TerminalId",
                schema: "public",
                table: "pos_sessions",
                newName: "IX_pos_sessions_terminal");

            migrationBuilder.RenameIndex(
                name: "IX_pos_payments_TransactionId",
                schema: "public",
                table: "pos_payments",
                newName: "IX_pos_payments_transaction");

            migrationBuilder.RenameIndex(
                name: "IX_loyalty_transactions_CustomerId_CreatedAt",
                schema: "public",
                table: "loyalty_transactions",
                newName: "IX_loyalty_transactions_customer_date");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entry_lines_JournalEntryId",
                schema: "public",
                table: "journal_entry_lines",
                newName: "IX_journal_entry_lines_journal_entry");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entry_lines_AccountId",
                schema: "public",
                table: "journal_entry_lines",
                newName: "IX_journal_entry_lines_account");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entries_EntryDate",
                schema: "public",
                table: "journal_entries",
                newName: "IX_journal_entries_entry_date");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_notes_SupplierId",
                schema: "public",
                table: "goods_receipt_notes",
                newName: "IX_goods_receipt_notes_supplier");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_notes_PurchaseOrderId",
                schema: "public",
                table: "goods_receipt_notes",
                newName: "IX_goods_receipt_notes_purchase_order");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_lines_PoLineId",
                schema: "public",
                table: "goods_receipt_lines",
                newName: "IX_goods_receipt_lines_po_line");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_lines_GrnId",
                schema: "public",
                table: "goods_receipt_lines",
                newName: "IX_goods_receipt_lines_grn");

            migrationBuilder.RenameIndex(
                name: "IX_fiscal_documents_DocumentType_Status",
                schema: "public",
                table: "fiscal_documents",
                newName: "IX_fiscal_documents_type_status");

            migrationBuilder.RenameIndex(
                name: "IX_daily_closings_StoreId_ClosingDate",
                schema: "public",
                table: "daily_closings",
                newName: "IX_daily_closings_store_date");

            migrationBuilder.RenameIndex(
                name: "IX_chart_of_accounts_ParentId",
                schema: "public",
                table: "chart_of_accounts",
                newName: "IX_chart_of_accounts_parent");

            migrationBuilder.RenameIndex(
                name: "IX_categories_ParentId",
                schema: "public",
                table: "categories",
                newName: "IX_categories_parent");

            migrationBuilder.RenameIndex(
                name: "IX_bank_accounts_GlAccountId",
                schema: "public",
                table: "bank_accounts",
                newName: "IX_bank_accounts_gl_account");

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                schema: "public",
                table: "products",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "audit_logs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EntityType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    EntityId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Action = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    ChangedProperties = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: true),
                    IpAddress = table.Column<string>(type: "character varying(45)", maxLength: 45, nullable: true),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "backup_records",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    FilePath = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Type = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    StartedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    InitiatedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    InitiatedByUserName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_backup_records", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "file_metadata",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    StoredFileName = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    Category = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    EntityId = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    UploadedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UploadedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_file_metadata", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receiving_orders",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Source = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    SupplierId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ReceivedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiving_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipping_orders",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShippingNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    OrderType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    SourceOrderId = table.Column<Guid>(type: "uuid", nullable: true),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DestWarehouseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ShippingAddress = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    TrackingNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Carrier = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpectedShipDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShippedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    DeliveredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ShippedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    RsGeWaybillId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_locations",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    NameKa = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    LocationType = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    ParentLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MaxCapacity = table.Column<int>(type: "integer", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_locations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_warehouse_locations_warehouse_locations_ParentLocationId",
                        column: x => x.ParentLocationId,
                        principalSchema: "public",
                        principalTable: "warehouse_locations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery_logs",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SubscriptionId = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
                    AttemptNumber = table.Column<int>(type: "integer", nullable: false),
                    HttpStatusCode = table.Column<int>(type: "integer", nullable: true),
                    ResponseBody = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    ErrorMessage = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                    Success = table.Column<bool>(type: "boolean", nullable: false),
                    AttemptedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DurationMs = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_delivery_logs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_subscriptions",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Secret = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    EventTypes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    MaxRetries = table.Column<int>(type: "integer", nullable: false, defaultValue: 3),
                    ConsecutiveFailures = table.Column<int>(type: "integer", nullable: false),
                    LastDeliveryAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LastDeliveryStatus = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_webhook_subscriptions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "receiving_order_lines",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReceivingOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExpectedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ReceivedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    DamagedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    LocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_receiving_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_receiving_order_lines_receiving_orders_ReceivingOrderId",
                        column: x => x.ReceivingOrderId,
                        principalSchema: "public",
                        principalTable: "receiving_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "shipping_order_lines",
                schema: "public",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShippingOrderId = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    VariantId = table.Column<Guid>(type: "uuid", nullable: true),
                    OrderedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PickedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PackedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    ShippedQty = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    PickLocationId = table.Column<Guid>(type: "uuid", nullable: true),
                    BatchNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SerialNumber = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipping_order_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_shipping_order_lines_shipping_orders_ShippingOrderId",
                        column: x => x.ShippingOrderId,
                        principalSchema: "public",
                        principalTable: "shipping_orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_active",
                schema: "public",
                table: "warehouses",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_linked_store",
                schema: "public",
                table: "warehouses",
                column: "LinkedStoreId",
                filter: "\"LinkedStoreId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_vat_declarations_status",
                schema: "public",
                table: "vat_declarations",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_orders_created_at",
                schema: "public",
                table: "transfer_orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_orders_dest_warehouse",
                schema: "public",
                table: "transfer_orders",
                column: "DestWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_orders_source_warehouse",
                schema: "public",
                table: "transfer_orders",
                column: "SourceWarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_orders_status",
                schema: "public",
                table: "transfer_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_transfer_order_lines_product",
                schema: "public",
                table: "transfer_order_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_active",
                schema: "public",
                table: "suppliers",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_suppliers_tin",
                schema: "public",
                table: "suppliers",
                column: "Tin",
                filter: "\"Tin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_created_at",
                schema: "public",
                table: "stock_movements",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_product_date",
                schema: "public",
                table: "stock_movements",
                columns: new[] { "ProductId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_reference",
                schema: "public",
                table: "stock_movements",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_type",
                schema: "public",
                table: "stock_movements",
                column: "MovementType");

            migrationBuilder.CreateIndex(
                name: "IX_stock_movements_warehouse",
                schema: "public",
                table: "stock_movements",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_product",
                schema: "public",
                table: "stock_levels",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_product_warehouse",
                schema: "public",
                table: "stock_levels",
                columns: new[] { "ProductId", "WarehouseId" });

            migrationBuilder.CreateIndex(
                name: "IX_stock_levels_warehouse",
                schema: "public",
                table: "stock_levels",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_status",
                schema: "public",
                table: "stock_counts",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_stock_counts_warehouse",
                schema: "public",
                table: "stock_counts",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_stock_count_lines_product",
                schema: "public",
                table: "stock_count_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_waybills_buyer_tin",
                schema: "public",
                table: "rsge_waybills",
                column: "BuyerTin",
                filter: "\"BuyerTin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_waybills_number",
                schema: "public",
                table: "rsge_waybills",
                column: "WaybillNumber",
                filter: "\"WaybillNumber\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_waybills_seller_tin",
                schema: "public",
                table: "rsge_waybills",
                column: "SellerTin");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_waybills_status",
                schema: "public",
                table: "rsge_waybills",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_comm_logs_created_at",
                schema: "public",
                table: "rsge_communication_logs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_comm_logs_http_status",
                schema: "public",
                table: "rsge_communication_logs",
                column: "HttpStatus");

            migrationBuilder.CreateIndex(
                name: "IX_rsge_comm_logs_operation",
                schema: "public",
                table: "rsge_communication_logs",
                column: "Operation");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_expires_at",
                schema: "public",
                table: "refresh_tokens",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens",
                column: "TokenHash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_created_at",
                schema: "public",
                table: "purchase_orders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_order_date",
                schema: "public",
                table: "purchase_orders",
                column: "OrderDate");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_status",
                schema: "public",
                table: "purchase_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_orders_warehouse",
                schema: "public",
                table: "purchase_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_purchase_order_lines_product",
                schema: "public",
                table: "purchase_order_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_promotions_active_validity",
                schema: "public",
                table: "promotions",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_products_active",
                schema: "public",
                table: "products",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_products_name",
                schema: "public",
                table: "products",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_active_validity",
                schema: "public",
                table: "price_lists",
                columns: new[] { "IsActive", "ValidFrom", "ValidTo" });

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_store",
                schema: "public",
                table: "price_lists",
                column: "StoreId",
                filter: "\"StoreId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_price_lists_type",
                schema: "public",
                table: "price_lists",
                column: "PriceType");

            migrationBuilder.CreateIndex(
                name: "IX_price_list_items_product",
                schema: "public",
                table: "price_list_items",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_pos_transactions_created_at",
                schema: "public",
                table: "pos_transactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_pos_transactions_customer",
                schema: "public",
                table: "pos_transactions",
                column: "CustomerId",
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_pos_transactions_status",
                schema: "public",
                table: "pos_transactions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_pos_transaction_lines_product",
                schema: "public",
                table: "pos_transaction_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_pos_terminals_store",
                schema: "public",
                table: "pos_terminals",
                column: "StoreId");

            migrationBuilder.CreateIndex(
                name: "IX_pos_sessions_cashier",
                schema: "public",
                table: "pos_sessions",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_pos_sessions_status",
                schema: "public",
                table: "pos_sessions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_pos_payments_method_date",
                schema: "public",
                table: "pos_payments",
                columns: new[] { "PaymentMethod", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_loyalty_transactions_type",
                schema: "public",
                table: "loyalty_transactions",
                column: "TransactionType");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_created_at",
                schema: "public",
                table: "journal_entries",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_source",
                schema: "public",
                table: "journal_entries",
                columns: new[] { "SourceType", "SourceId" });

            migrationBuilder.CreateIndex(
                name: "IX_journal_entries_status",
                schema: "public",
                table: "journal_entries",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_receipt_date",
                schema: "public",
                table: "goods_receipt_notes",
                column: "ReceiptDate");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_status",
                schema: "public",
                table: "goods_receipt_notes",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_notes_warehouse",
                schema: "public",
                table: "goods_receipt_notes",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_goods_receipt_lines_product",
                schema: "public",
                table: "goods_receipt_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_created_at",
                schema: "public",
                table: "fiscal_documents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_deadline",
                schema: "public",
                table: "fiscal_documents",
                column: "SubmissionDeadline");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_pending_status",
                schema: "public",
                table: "fiscal_documents",
                column: "Status",
                filter: "\"Status\" IN ('PENDING', 'QUEUED', 'FAILED')");

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_reference",
                schema: "public",
                table: "fiscal_documents",
                columns: new[] { "ReferenceType", "ReferenceId" });

            migrationBuilder.CreateIndex(
                name: "IX_fiscal_documents_rsge_id",
                schema: "public",
                table: "fiscal_documents",
                column: "RsGeId",
                filter: "\"RsGeId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_daily_closings_status",
                schema: "public",
                table: "daily_closings",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_customers_email",
                schema: "public",
                table: "customers",
                column: "Email",
                filter: "\"Email\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_loyalty_tier",
                schema: "public",
                table: "customers",
                column: "LoyaltyTier");

            migrationBuilder.CreateIndex(
                name: "IX_customers_phone",
                schema: "public",
                table: "customers",
                column: "Phone",
                filter: "\"Phone\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_customers_tin",
                schema: "public",
                table: "customers",
                column: "Tin",
                filter: "\"Tin\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_chart_of_accounts_type",
                schema: "public",
                table: "chart_of_accounts",
                column: "AccountType");

            migrationBuilder.CreateIndex(
                name: "IX_bank_accounts_iban",
                schema: "public",
                table: "bank_accounts",
                column: "Iban",
                filter: "\"Iban\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_EntityType_EntityId",
                schema: "public",
                table: "audit_logs",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_Timestamp",
                schema: "public",
                table: "audit_logs",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_audit_logs_UserId",
                schema: "public",
                table: "audit_logs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_backup_records_StartedAt",
                schema: "public",
                table: "backup_records",
                column: "StartedAt");

            migrationBuilder.CreateIndex(
                name: "IX_backup_records_Status",
                schema: "public",
                table: "backup_records",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_file_metadata_entity",
                schema: "public",
                table: "file_metadata",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_file_metadata_StoredFileName",
                schema: "public",
                table: "file_metadata",
                column: "StoredFileName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_file_metadata_uploader",
                schema: "public",
                table: "file_metadata",
                column: "UploadedBy");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_order_lines_product",
                schema: "public",
                table: "receiving_order_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_order_lines_ReceivingOrderId",
                schema: "public",
                table: "receiving_order_lines",
                column: "ReceivingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_orders_ReceivingNumber",
                schema: "public",
                table: "receiving_orders",
                column: "ReceivingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receiving_orders_source",
                schema: "public",
                table: "receiving_orders",
                column: "SourceOrderId",
                filter: "\"SourceOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_orders_status",
                schema: "public",
                table: "receiving_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_receiving_orders_warehouse",
                schema: "public",
                table: "receiving_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_order_lines_product",
                schema: "public",
                table: "shipping_order_lines",
                column: "ProductId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_order_lines_ShippingOrderId",
                schema: "public",
                table: "shipping_order_lines",
                column: "ShippingOrderId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_customer",
                schema: "public",
                table: "shipping_orders",
                column: "CustomerId",
                filter: "\"CustomerId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_ShippingNumber",
                schema: "public",
                table: "shipping_orders",
                column: "ShippingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_source",
                schema: "public",
                table: "shipping_orders",
                column: "SourceOrderId",
                filter: "\"SourceOrderId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_status",
                schema: "public",
                table: "shipping_orders",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_warehouse",
                schema: "public",
                table: "shipping_orders",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_shipping_orders_waybill",
                schema: "public",
                table: "shipping_orders",
                column: "RsGeWaybillId",
                filter: "\"RsGeWaybillId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_locations_parent",
                schema: "public",
                table: "warehouse_locations",
                column: "ParentLocationId",
                filter: "\"ParentLocationId\" IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_locations_warehouse",
                schema: "public",
                table: "warehouse_locations",
                column: "WarehouseId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_locations_warehouse_code",
                schema: "public",
                table: "warehouse_locations",
                columns: new[] { "WarehouseId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_logs_attempted_at",
                schema: "public",
                table: "webhook_delivery_logs",
                column: "AttemptedAt");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_delivery_logs_subscription",
                schema: "public",
                table: "webhook_delivery_logs",
                column: "SubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_webhook_subscriptions_active",
                schema: "public",
                table: "webhook_subscriptions",
                column: "IsActive");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "backup_records",
                schema: "public");

            migrationBuilder.DropTable(
                name: "file_metadata",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receiving_order_lines",
                schema: "public");

            migrationBuilder.DropTable(
                name: "shipping_order_lines",
                schema: "public");

            migrationBuilder.DropTable(
                name: "warehouse_locations",
                schema: "public");

            migrationBuilder.DropTable(
                name: "webhook_delivery_logs",
                schema: "public");

            migrationBuilder.DropTable(
                name: "webhook_subscriptions",
                schema: "public");

            migrationBuilder.DropTable(
                name: "receiving_orders",
                schema: "public");

            migrationBuilder.DropTable(
                name: "shipping_orders",
                schema: "public");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_active",
                schema: "public",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_warehouses_linked_store",
                schema: "public",
                table: "warehouses");

            migrationBuilder.DropIndex(
                name: "IX_vat_declarations_status",
                schema: "public",
                table: "vat_declarations");

            migrationBuilder.DropIndex(
                name: "IX_transfer_orders_created_at",
                schema: "public",
                table: "transfer_orders");

            migrationBuilder.DropIndex(
                name: "IX_transfer_orders_dest_warehouse",
                schema: "public",
                table: "transfer_orders");

            migrationBuilder.DropIndex(
                name: "IX_transfer_orders_source_warehouse",
                schema: "public",
                table: "transfer_orders");

            migrationBuilder.DropIndex(
                name: "IX_transfer_orders_status",
                schema: "public",
                table: "transfer_orders");

            migrationBuilder.DropIndex(
                name: "IX_transfer_order_lines_product",
                schema: "public",
                table: "transfer_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_active",
                schema: "public",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_suppliers_tin",
                schema: "public",
                table: "suppliers");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_created_at",
                schema: "public",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_product_date",
                schema: "public",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_reference",
                schema: "public",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_type",
                schema: "public",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_movements_warehouse",
                schema: "public",
                table: "stock_movements");

            migrationBuilder.DropIndex(
                name: "IX_stock_levels_product",
                schema: "public",
                table: "stock_levels");

            migrationBuilder.DropIndex(
                name: "IX_stock_levels_product_warehouse",
                schema: "public",
                table: "stock_levels");

            migrationBuilder.DropIndex(
                name: "IX_stock_levels_warehouse",
                schema: "public",
                table: "stock_levels");

            migrationBuilder.DropIndex(
                name: "IX_stock_counts_status",
                schema: "public",
                table: "stock_counts");

            migrationBuilder.DropIndex(
                name: "IX_stock_counts_warehouse",
                schema: "public",
                table: "stock_counts");

            migrationBuilder.DropIndex(
                name: "IX_stock_count_lines_product",
                schema: "public",
                table: "stock_count_lines");

            migrationBuilder.DropIndex(
                name: "IX_rsge_waybills_buyer_tin",
                schema: "public",
                table: "rsge_waybills");

            migrationBuilder.DropIndex(
                name: "IX_rsge_waybills_number",
                schema: "public",
                table: "rsge_waybills");

            migrationBuilder.DropIndex(
                name: "IX_rsge_waybills_seller_tin",
                schema: "public",
                table: "rsge_waybills");

            migrationBuilder.DropIndex(
                name: "IX_rsge_waybills_status",
                schema: "public",
                table: "rsge_waybills");

            migrationBuilder.DropIndex(
                name: "IX_rsge_comm_logs_created_at",
                schema: "public",
                table: "rsge_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_rsge_comm_logs_http_status",
                schema: "public",
                table: "rsge_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_rsge_comm_logs_operation",
                schema: "public",
                table: "rsge_communication_logs");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_expires_at",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_token_hash",
                schema: "public",
                table: "refresh_tokens");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_created_at",
                schema: "public",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_order_date",
                schema: "public",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_status",
                schema: "public",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_orders_warehouse",
                schema: "public",
                table: "purchase_orders");

            migrationBuilder.DropIndex(
                name: "IX_purchase_order_lines_product",
                schema: "public",
                table: "purchase_order_lines");

            migrationBuilder.DropIndex(
                name: "IX_promotions_active_validity",
                schema: "public",
                table: "promotions");

            migrationBuilder.DropIndex(
                name: "IX_products_active",
                schema: "public",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_products_name",
                schema: "public",
                table: "products");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_active_validity",
                schema: "public",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_store",
                schema: "public",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_lists_type",
                schema: "public",
                table: "price_lists");

            migrationBuilder.DropIndex(
                name: "IX_price_list_items_product",
                schema: "public",
                table: "price_list_items");

            migrationBuilder.DropIndex(
                name: "IX_pos_transactions_created_at",
                schema: "public",
                table: "pos_transactions");

            migrationBuilder.DropIndex(
                name: "IX_pos_transactions_customer",
                schema: "public",
                table: "pos_transactions");

            migrationBuilder.DropIndex(
                name: "IX_pos_transactions_status",
                schema: "public",
                table: "pos_transactions");

            migrationBuilder.DropIndex(
                name: "IX_pos_transaction_lines_product",
                schema: "public",
                table: "pos_transaction_lines");

            migrationBuilder.DropIndex(
                name: "IX_pos_terminals_store",
                schema: "public",
                table: "pos_terminals");

            migrationBuilder.DropIndex(
                name: "IX_pos_sessions_cashier",
                schema: "public",
                table: "pos_sessions");

            migrationBuilder.DropIndex(
                name: "IX_pos_sessions_status",
                schema: "public",
                table: "pos_sessions");

            migrationBuilder.DropIndex(
                name: "IX_pos_payments_method_date",
                schema: "public",
                table: "pos_payments");

            migrationBuilder.DropIndex(
                name: "IX_loyalty_transactions_type",
                schema: "public",
                table: "loyalty_transactions");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_created_at",
                schema: "public",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_source",
                schema: "public",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_journal_entries_status",
                schema: "public",
                table: "journal_entries");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_notes_receipt_date",
                schema: "public",
                table: "goods_receipt_notes");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_notes_status",
                schema: "public",
                table: "goods_receipt_notes");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_notes_warehouse",
                schema: "public",
                table: "goods_receipt_notes");

            migrationBuilder.DropIndex(
                name: "IX_goods_receipt_lines_product",
                schema: "public",
                table: "goods_receipt_lines");

            migrationBuilder.DropIndex(
                name: "IX_fiscal_documents_created_at",
                schema: "public",
                table: "fiscal_documents");

            migrationBuilder.DropIndex(
                name: "IX_fiscal_documents_deadline",
                schema: "public",
                table: "fiscal_documents");

            migrationBuilder.DropIndex(
                name: "IX_fiscal_documents_pending_status",
                schema: "public",
                table: "fiscal_documents");

            migrationBuilder.DropIndex(
                name: "IX_fiscal_documents_reference",
                schema: "public",
                table: "fiscal_documents");

            migrationBuilder.DropIndex(
                name: "IX_fiscal_documents_rsge_id",
                schema: "public",
                table: "fiscal_documents");

            migrationBuilder.DropIndex(
                name: "IX_daily_closings_status",
                schema: "public",
                table: "daily_closings");

            migrationBuilder.DropIndex(
                name: "IX_customers_email",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_loyalty_tier",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_phone",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_customers_tin",
                schema: "public",
                table: "customers");

            migrationBuilder.DropIndex(
                name: "IX_chart_of_accounts_type",
                schema: "public",
                table: "chart_of_accounts");

            migrationBuilder.DropIndex(
                name: "IX_bank_accounts_iban",
                schema: "public",
                table: "bank_accounts");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                schema: "public",
                table: "products");

            migrationBuilder.RenameIndex(
                name: "IX_vat_declarations_period",
                schema: "public",
                table: "vat_declarations",
                newName: "IX_vat_declarations_PeriodStart_PeriodEnd");

            migrationBuilder.RenameIndex(
                name: "IX_transfer_order_lines_transfer_order",
                schema: "public",
                table: "transfer_order_lines",
                newName: "IX_transfer_order_lines_TransferOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_stock_movements_product_warehouse",
                schema: "public",
                table: "stock_movements",
                newName: "IX_stock_movements_ProductId_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_stock_levels_product_variant_warehouse",
                schema: "public",
                table: "stock_levels",
                newName: "IX_stock_levels_ProductId_VariantId_WarehouseId");

            migrationBuilder.RenameIndex(
                name: "IX_stock_count_lines_stock_count",
                schema: "public",
                table: "stock_count_lines",
                newName: "IX_stock_count_lines_StockCountId");

            migrationBuilder.RenameIndex(
                name: "IX_rsge_comm_logs_fiscal_document",
                schema: "public",
                table: "rsge_communication_logs",
                newName: "IX_rsge_communication_logs_FiscalDocumentId");

            migrationBuilder.RenameIndex(
                name: "IX_rsge_comm_logs_correlation",
                schema: "public",
                table: "rsge_communication_logs",
                newName: "IX_rsge_communication_logs_CorrelationId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_user",
                schema: "public",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_orders_supplier",
                schema: "public",
                table: "purchase_orders",
                newName: "IX_purchase_orders_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_purchase_order_lines_purchase_order",
                schema: "public",
                table: "purchase_order_lines",
                newName: "IX_purchase_order_lines_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_products_category",
                schema: "public",
                table: "products",
                newName: "IX_products_CategoryId");

            migrationBuilder.RenameIndex(
                name: "IX_product_variants_product",
                schema: "public",
                table: "product_variants",
                newName: "IX_product_variants_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_product_barcodes_product",
                schema: "public",
                table: "product_barcodes",
                newName: "IX_product_barcodes_ProductId");

            migrationBuilder.RenameIndex(
                name: "IX_price_list_items_list_product_variant",
                schema: "public",
                table: "price_list_items",
                newName: "IX_price_list_items_PriceListId_ProductId_VariantId");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transactions_store_date",
                schema: "public",
                table: "pos_transactions",
                newName: "IX_pos_transactions_StoreId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transactions_session",
                schema: "public",
                table: "pos_transactions",
                newName: "IX_pos_transactions_SessionId");

            migrationBuilder.RenameIndex(
                name: "IX_pos_transaction_lines_transaction",
                schema: "public",
                table: "pos_transaction_lines",
                newName: "IX_pos_transaction_lines_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_pos_sessions_terminal",
                schema: "public",
                table: "pos_sessions",
                newName: "IX_pos_sessions_TerminalId");

            migrationBuilder.RenameIndex(
                name: "IX_pos_payments_transaction",
                schema: "public",
                table: "pos_payments",
                newName: "IX_pos_payments_TransactionId");

            migrationBuilder.RenameIndex(
                name: "IX_loyalty_transactions_customer_date",
                schema: "public",
                table: "loyalty_transactions",
                newName: "IX_loyalty_transactions_CustomerId_CreatedAt");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entry_lines_journal_entry",
                schema: "public",
                table: "journal_entry_lines",
                newName: "IX_journal_entry_lines_JournalEntryId");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entry_lines_account",
                schema: "public",
                table: "journal_entry_lines",
                newName: "IX_journal_entry_lines_AccountId");

            migrationBuilder.RenameIndex(
                name: "IX_journal_entries_entry_date",
                schema: "public",
                table: "journal_entries",
                newName: "IX_journal_entries_EntryDate");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_notes_supplier",
                schema: "public",
                table: "goods_receipt_notes",
                newName: "IX_goods_receipt_notes_SupplierId");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_notes_purchase_order",
                schema: "public",
                table: "goods_receipt_notes",
                newName: "IX_goods_receipt_notes_PurchaseOrderId");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_lines_po_line",
                schema: "public",
                table: "goods_receipt_lines",
                newName: "IX_goods_receipt_lines_PoLineId");

            migrationBuilder.RenameIndex(
                name: "IX_goods_receipt_lines_grn",
                schema: "public",
                table: "goods_receipt_lines",
                newName: "IX_goods_receipt_lines_GrnId");

            migrationBuilder.RenameIndex(
                name: "IX_fiscal_documents_type_status",
                schema: "public",
                table: "fiscal_documents",
                newName: "IX_fiscal_documents_DocumentType_Status");

            migrationBuilder.RenameIndex(
                name: "IX_daily_closings_store_date",
                schema: "public",
                table: "daily_closings",
                newName: "IX_daily_closings_StoreId_ClosingDate");

            migrationBuilder.RenameIndex(
                name: "IX_chart_of_accounts_parent",
                schema: "public",
                table: "chart_of_accounts",
                newName: "IX_chart_of_accounts_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_categories_parent",
                schema: "public",
                table: "categories",
                newName: "IX_categories_ParentId");

            migrationBuilder.RenameIndex(
                name: "IX_bank_accounts_gl_account",
                schema: "public",
                table: "bank_accounts",
                newName: "IX_bank_accounts_GlAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouses_LinkedStoreId",
                schema: "public",
                table: "warehouses",
                column: "LinkedStoreId");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_TokenHash",
                schema: "public",
                table: "refresh_tokens",
                column: "TokenHash");
        }
    }
}
