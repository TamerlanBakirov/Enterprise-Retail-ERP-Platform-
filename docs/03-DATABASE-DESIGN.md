# Database Design Document

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Draft — Awaiting Review

---

## 1. Database Strategy

- **Engine**: PostgreSQL 17
- **Multi-Tenancy**: Schema-per-company
- **Encoding**: UTF-8 (full Georgian ქართული support)
- **Locale**: ka_GE.UTF-8 (Georgian) for collation
- **Timezone**: All timestamps stored as TIMESTAMPTZ (UTC), displayed in Asia/Tbilisi (UTC+4)
- **Monetary**: DECIMAL(18,2) for all GEL amounts — never FLOAT/DOUBLE

---

## 2. Core Entity Relationship Diagram

```
┌──────────┐     ┌──────────┐     ┌──────────────┐
│ Company  │────<│  Store    │────<│ POS Terminal │
└──────────┘     └──────────┘     └──────────────┘
     │                │                    │
     │           ┌────▼────┐         ┌─────▼──────┐
     │           │Warehouse│         │POS Session │
     │           └─────────┘         └─────┬──────┘
     │                │                    │
     │           ┌────▼──────┐       ┌─────▼──────────┐
     │           │Stock Level│       │POS Transaction  │
     │           └───────────┘       └─────┬──────────┘
     │                                     │
┌────▼────┐  ┌──────────┐           ┌─────▼──────────┐
│ Product │──│ Barcode  │           │Transaction Line│
└────┬────┘  └──────────┘           └────────────────┘
     │
┌────▼─────────┐  ┌──────────────┐  ┌────────────────┐
│Product Variant│  │  Price List  │  │ Promotion      │
└──────────────┘  └──────────────┘  └────────────────┘
```

---

## 3. Table Definitions

### 3.1 Company & Organization

```sql
-- Shared schema
CREATE TABLE shared.companies (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(20) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200) NOT NULL,          -- Georgian name
    tin             VARCHAR(20) NOT NULL UNIQUE,     -- RS.GE TIN (9 digits)
    is_vat_payer    BOOLEAN NOT NULL DEFAULT FALSE,
    vat_registration_date DATE,
    legal_address   TEXT,
    actual_address  TEXT,
    phone           VARCHAR(50),
    email           VARCHAR(200),
    settings        JSONB NOT NULL DEFAULT '{}',
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Per-company schema
CREATE TABLE stores (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(20) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    store_type      VARCHAR(50) NOT NULL,            -- RETAIL, OUTLET, FRANCHISE
    address         TEXT NOT NULL,
    city            VARCHAR(100) NOT NULL,
    region          VARCHAR(100),
    phone           VARCHAR(50),
    manager_user_id UUID REFERENCES users(id),
    latitude        DECIMAL(10,7),
    longitude       DECIMAL(10,7),
    timezone        VARCHAR(50) NOT NULL DEFAULT 'Asia/Tbilisi',
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    settings        JSONB NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE warehouses (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(20) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    warehouse_type  VARCHAR(50) NOT NULL,            -- CENTRAL, REGIONAL, STORE
    address         TEXT NOT NULL,
    city            VARCHAR(100) NOT NULL,
    region          VARCHAR(100),
    linked_store_id UUID REFERENCES stores(id),      -- NULL for central warehouses
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    settings        JSONB NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 3.2 Users & Access Control

```sql
CREATE TABLE users (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    username        VARCHAR(100) NOT NULL UNIQUE,
    email           VARCHAR(200) NOT NULL UNIQUE,
    password_hash   VARCHAR(500) NOT NULL,
    first_name      VARCHAR(100) NOT NULL,
    last_name       VARCHAR(100) NOT NULL,
    first_name_ka   VARCHAR(100),
    last_name_ka    VARCHAR(100),
    phone           VARCHAR(50),
    default_store_id UUID REFERENCES stores(id),
    default_language VARCHAR(5) NOT NULL DEFAULT 'ka',  -- ka, en
    is_2fa_enabled  BOOLEAN NOT NULL DEFAULT FALSE,
    totp_secret     VARCHAR(200),
    failed_login_count INT NOT NULL DEFAULT 0,
    locked_until    TIMESTAMPTZ,
    last_login_at   TIMESTAMPTZ,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE roles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(100) NOT NULL,
    name_ka         VARCHAR(100),
    description     TEXT,
    is_system       BOOLEAN NOT NULL DEFAULT FALSE,  -- system roles cannot be deleted
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE permissions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    module          VARCHAR(50) NOT NULL,             -- POS, INVENTORY, FINANCE, etc.
    action          VARCHAR(50) NOT NULL,             -- READ, CREATE, UPDATE, DELETE, APPROVE
    resource        VARCHAR(100) NOT NULL,            -- products, transactions, etc.
    UNIQUE(module, action, resource)
);

CREATE TABLE role_permissions (
    role_id         UUID NOT NULL REFERENCES roles(id),
    permission_id   UUID NOT NULL REFERENCES permissions(id),
    PRIMARY KEY (role_id, permission_id)
);

CREATE TABLE user_roles (
    user_id         UUID NOT NULL REFERENCES users(id),
    role_id         UUID NOT NULL REFERENCES roles(id),
    store_id        UUID REFERENCES stores(id),       -- NULL = all stores
    PRIMARY KEY (user_id, role_id, COALESCE(store_id, '00000000-0000-0000-0000-000000000000'))
);

CREATE TABLE refresh_tokens (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id         UUID NOT NULL REFERENCES users(id),
    token_hash      VARCHAR(500) NOT NULL UNIQUE,
    device_info     JSONB,
    ip_address      INET,
    expires_at      TIMESTAMPTZ NOT NULL,
    revoked_at      TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 3.3 Product Management

```sql
CREATE TABLE categories (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    parent_id       UUID REFERENCES categories(id),
    code            VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    sort_order      INT NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE products (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    sku             VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(300) NOT NULL,
    name_ka         VARCHAR(300),
    description     TEXT,
    category_id     UUID NOT NULL REFERENCES categories(id),
    unit_of_measure VARCHAR(20) NOT NULL DEFAULT 'PCS',  -- PCS, KG, L, M, etc.
    rsge_unit_id    INT,                                  -- RS.GE unit reference
    vat_applicable  BOOLEAN NOT NULL DEFAULT TRUE,
    excise_code     VARCHAR(20),                          -- RS.GE excise code if applicable
    weight_kg       DECIMAL(10,3),
    volume_l        DECIMAL(10,3),
    width_cm        DECIMAL(10,2),
    height_cm       DECIMAL(10,2),
    depth_cm        DECIMAL(10,2),
    min_stock_level DECIMAL(18,3) DEFAULT 0,
    max_stock_level DECIMAL(18,3),
    reorder_point   DECIMAL(18,3),
    reorder_qty     DECIMAL(18,3),
    is_serialized   BOOLEAN NOT NULL DEFAULT FALSE,
    is_batch_tracked BOOLEAN NOT NULL DEFAULT FALSE,
    has_expiry      BOOLEAN NOT NULL DEFAULT FALSE,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID NOT NULL REFERENCES users(id),
    updated_by      UUID NOT NULL REFERENCES users(id)
);

CREATE TABLE product_variants (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      UUID NOT NULL REFERENCES products(id),
    sku             VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(300) NOT NULL,
    attributes      JSONB NOT NULL DEFAULT '{}',      -- {"color": "red", "size": "XL"}
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE product_barcodes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      UUID REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    barcode         VARCHAR(100) NOT NULL,
    barcode_type    VARCHAR(20) NOT NULL DEFAULT 'EAN13', -- EAN13, EAN8, UPC, CODE128, INTERNAL
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    UNIQUE(barcode)
);

CREATE TABLE product_images (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      UUID NOT NULL REFERENCES products(id),
    url             VARCHAR(500) NOT NULL,
    sort_order      INT NOT NULL DEFAULT 0,
    is_primary      BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE product_bundles (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    bundle_product_id UUID NOT NULL REFERENCES products(id),
    component_product_id UUID NOT NULL REFERENCES products(id),
    quantity        DECIMAL(18,3) NOT NULL,
    UNIQUE(bundle_product_id, component_product_id)
);
```

### 3.4 Pricing

```sql
CREATE TABLE price_lists (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    currency        VARCHAR(3) NOT NULL DEFAULT 'GEL',
    price_type      VARCHAR(20) NOT NULL,             -- RETAIL, WHOLESALE, EMPLOYEE, COST
    store_id        UUID REFERENCES stores(id),       -- NULL = all stores
    valid_from      TIMESTAMPTZ NOT NULL,
    valid_to        TIMESTAMPTZ,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    priority        INT NOT NULL DEFAULT 0,           -- higher = higher priority
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE price_list_items (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    price_list_id   UUID NOT NULL REFERENCES price_lists(id),
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    price           DECIMAL(18,2) NOT NULL,
    min_qty         DECIMAL(18,3) NOT NULL DEFAULT 1,
    UNIQUE(price_list_id, product_id, variant_id, min_qty)
);

CREATE TABLE promotions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    promotion_type  VARCHAR(50) NOT NULL,             -- PERCENTAGE, FIXED, BOGO, BUNDLE
    discount_value  DECIMAL(18,2),
    conditions      JSONB NOT NULL DEFAULT '{}',      -- flexible rule conditions
    store_ids       UUID[],                           -- NULL = all stores
    valid_from      TIMESTAMPTZ NOT NULL,
    valid_to        TIMESTAMPTZ NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    max_uses        INT,
    current_uses    INT NOT NULL DEFAULT 0,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 3.5 Inventory

```sql
CREATE TABLE stock_levels (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    warehouse_id    UUID NOT NULL REFERENCES warehouses(id),
    location_code   VARCHAR(50),                      -- bin/shelf location
    quantity_on_hand DECIMAL(18,3) NOT NULL DEFAULT 0,
    quantity_reserved DECIMAL(18,3) NOT NULL DEFAULT 0,
    quantity_in_transit DECIMAL(18,3) NOT NULL DEFAULT 0,
    cost_price      DECIMAL(18,2) NOT NULL DEFAULT 0, -- weighted average cost
    last_count_date TIMESTAMPTZ,
    row_version     INT NOT NULL DEFAULT 1,
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(product_id, variant_id, warehouse_id, location_code)
) PARTITION BY LIST (warehouse_id);

CREATE TABLE stock_movements (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    movement_type   VARCHAR(30) NOT NULL,             -- RECEIPT, DISPATCH, TRANSFER_IN, TRANSFER_OUT, ADJUSTMENT, SALE, RETURN
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    warehouse_id    UUID NOT NULL REFERENCES warehouses(id),
    quantity        DECIMAL(18,3) NOT NULL,           -- positive = in, negative = out
    cost_price      DECIMAL(18,2) NOT NULL,
    reference_type  VARCHAR(50) NOT NULL,             -- POS_TRANSACTION, TRANSFER_ORDER, PURCHASE_ORDER, ADJUSTMENT
    reference_id    UUID NOT NULL,
    batch_number    VARCHAR(100),
    serial_number   VARCHAR(100),
    expiry_date     DATE,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID NOT NULL REFERENCES users(id)
) PARTITION BY RANGE (created_at);

CREATE TABLE stock_counts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    warehouse_id    UUID NOT NULL REFERENCES warehouses(id),
    count_type      VARCHAR(20) NOT NULL,             -- FULL, PARTIAL, CYCLE
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT', -- DRAFT, IN_PROGRESS, COMPLETED, CANCELLED
    started_at      TIMESTAMPTZ,
    completed_at    TIMESTAMPTZ,
    created_by      UUID NOT NULL REFERENCES users(id),
    approved_by     UUID REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE stock_count_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    stock_count_id  UUID NOT NULL REFERENCES stock_counts(id),
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    expected_qty    DECIMAL(18,3) NOT NULL,
    counted_qty     DECIMAL(18,3),
    variance        DECIMAL(18,3) GENERATED ALWAYS AS (counted_qty - expected_qty) STORED,
    counted_by      UUID REFERENCES users(id),
    counted_at      TIMESTAMPTZ
);

CREATE TABLE transfer_orders (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transfer_number VARCHAR(50) NOT NULL UNIQUE,
    source_warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    dest_warehouse_id UUID NOT NULL REFERENCES warehouses(id),
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    rsge_waybill_id UUID,                            -- linked RS.GE waybill
    requested_by    UUID NOT NULL REFERENCES users(id),
    approved_by     UUID REFERENCES users(id),
    shipped_at      TIMESTAMPTZ,
    received_at     TIMESTAMPTZ,
    notes           TEXT,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE transfer_order_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transfer_order_id UUID NOT NULL REFERENCES transfer_orders(id),
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    requested_qty   DECIMAL(18,3) NOT NULL,
    shipped_qty     DECIMAL(18,3),
    received_qty    DECIMAL(18,3),
    batch_number    VARCHAR(100),
    serial_number   VARCHAR(100)
);
```

### 3.6 POS (Point of Sale)

```sql
CREATE TABLE pos_terminals (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(30) NOT NULL UNIQUE,
    store_id        UUID NOT NULL REFERENCES stores(id),
    name            VARCHAR(100) NOT NULL,
    terminal_type   VARCHAR(20) NOT NULL DEFAULT 'REGISTER', -- REGISTER, SELF_SERVICE, MOBILE
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    settings        JSONB NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE pos_sessions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    terminal_id     UUID NOT NULL REFERENCES pos_terminals(id),
    cashier_id      UUID NOT NULL REFERENCES users(id),
    opened_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    closed_at       TIMESTAMPTZ,
    opening_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    closing_balance DECIMAL(18,2),
    expected_balance DECIMAL(18,2),
    cash_difference DECIMAL(18,2),
    status          VARCHAR(20) NOT NULL DEFAULT 'OPEN',  -- OPEN, CLOSED, RECONCILED
    notes           TEXT
);

CREATE TABLE pos_transactions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_number VARCHAR(50) NOT NULL UNIQUE,
    session_id      UUID NOT NULL REFERENCES pos_sessions(id),
    store_id        UUID NOT NULL REFERENCES stores(id),
    customer_id     UUID REFERENCES customers(id),
    transaction_type VARCHAR(20) NOT NULL,            -- SALE, RETURN, EXCHANGE, VOID
    subtotal        DECIMAL(18,2) NOT NULL,
    discount_total  DECIMAL(18,2) NOT NULL DEFAULT 0,
    vat_total       DECIMAL(18,2) NOT NULL,
    total           DECIMAL(18,2) NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'COMPLETED',
    fiscal_receipt_id UUID,                           -- link to RS.GE fiscal document
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID NOT NULL REFERENCES users(id),
    voided_at       TIMESTAMPTZ,
    voided_by       UUID REFERENCES users(id),
    void_reason     TEXT
) PARTITION BY RANGE (created_at);

CREATE TABLE pos_transaction_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id  UUID NOT NULL,                    -- references pos_transactions (partitioned)
    line_number     INT NOT NULL,
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    barcode         VARCHAR(100),
    product_name    VARCHAR(300) NOT NULL,            -- denormalized for receipt
    quantity        DECIMAL(18,3) NOT NULL,
    unit_price      DECIMAL(18,2) NOT NULL,
    discount_amount DECIMAL(18,2) NOT NULL DEFAULT 0,
    discount_reason VARCHAR(200),
    vat_amount      DECIMAL(18,2) NOT NULL,
    line_total      DECIMAL(18,2) NOT NULL,
    cost_price      DECIMAL(18,2) NOT NULL,           -- for margin calculation
    promotion_id    UUID REFERENCES promotions(id)
);

CREATE TABLE pos_payments (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    transaction_id  UUID NOT NULL,                    -- references pos_transactions
    payment_method  VARCHAR(30) NOT NULL,             -- CASH, CARD, BANK_TRANSFER, LOYALTY, MIXED
    amount          DECIMAL(18,2) NOT NULL,
    currency        VARCHAR(3) NOT NULL DEFAULT 'GEL',
    reference       VARCHAR(200),                     -- card auth code, transfer ref, etc.
    terminal_ref    VARCHAR(100),                     -- payment terminal reference
    change_amount   DECIMAL(18,2) DEFAULT 0,          -- cash change given
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE daily_closings (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    store_id        UUID NOT NULL REFERENCES stores(id),
    closing_date    DATE NOT NULL,
    total_sales     DECIMAL(18,2) NOT NULL,
    total_returns   DECIMAL(18,2) NOT NULL,
    total_vat       DECIMAL(18,2) NOT NULL,
    cash_total      DECIMAL(18,2) NOT NULL,
    card_total      DECIMAL(18,2) NOT NULL,
    other_total     DECIMAL(18,2) NOT NULL,
    transaction_count INT NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    closed_by       UUID REFERENCES users(id),
    closed_at       TIMESTAMPTZ,
    UNIQUE(store_id, closing_date)
);
```

### 3.7 Compliance & RS.GE

```sql
CREATE TABLE fiscal_documents (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    document_type   VARCHAR(30) NOT NULL,             -- WAYBILL, INVOICE, VAT_DECLARATION, FISCAL_RECEIPT
    document_number VARCHAR(100),                     -- RS.GE assigned number
    internal_ref    VARCHAR(100) NOT NULL,            -- internal reference
    reference_type  VARCHAR(50) NOT NULL,             -- POS_TRANSACTION, TRANSFER_ORDER, PURCHASE_ORDER
    reference_id    UUID NOT NULL,
    status          VARCHAR(30) NOT NULL DEFAULT 'PENDING',
    -- PENDING, QUEUED, SUBMITTED, CONFIRMED, REJECTED, FAILED, CANCELLED
    rsge_id         VARCHAR(100),                     -- RS.GE document ID
    rsge_status     VARCHAR(50),                      -- RS.GE status
    submission_deadline TIMESTAMPTZ NOT NULL,          -- 30-day deadline for invoices
    submitted_at    TIMESTAMPTZ,
    confirmed_at    TIMESTAMPTZ,
    retry_count     INT NOT NULL DEFAULT 0,
    last_error      TEXT,
    document_data   JSONB NOT NULL,                   -- full document payload
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE rsge_waybills (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fiscal_document_id UUID NOT NULL REFERENCES fiscal_documents(id),
    waybill_number  VARCHAR(50),                      -- RS.GE waybill number
    waybill_type    INT NOT NULL,                     -- RS.GE waybill type code
    seller_tin      VARCHAR(20) NOT NULL,
    seller_name     VARCHAR(200) NOT NULL,
    buyer_tin       VARCHAR(20),
    buyer_name      VARCHAR(200),
    transporter_tin VARCHAR(20),
    transport_type  INT,                              -- RS.GE transport type code
    vehicle_number  VARCHAR(20),
    driver_tin      VARCHAR(20),
    start_address   TEXT NOT NULL,
    end_address     TEXT NOT NULL,
    goods_data      JSONB NOT NULL,                   -- array of goods items
    total_amount    DECIMAL(18,2) NOT NULL,
    activate_date   TIMESTAMPTZ,
    delivery_date   TIMESTAMPTZ,
    status          VARCHAR(30) NOT NULL DEFAULT 'DRAFT',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE rsge_communication_log (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    fiscal_document_id UUID REFERENCES fiscal_documents(id),
    operation       VARCHAR(100) NOT NULL,            -- save_waybill, confirm_waybill, etc.
    direction       VARCHAR(10) NOT NULL,             -- REQUEST, RESPONSE
    endpoint        VARCHAR(500) NOT NULL,
    request_payload TEXT,                             -- SOAP XML request
    response_payload TEXT,                            -- SOAP XML response
    http_status     INT,
    duration_ms     INT,
    error_message   TEXT,
    correlation_id  UUID NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
) PARTITION BY RANGE (created_at);

CREATE TABLE vat_declarations (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    period_start    DATE NOT NULL,
    period_end      DATE NOT NULL,
    total_output_vat DECIMAL(18,2) NOT NULL,          -- VAT collected on sales
    total_input_vat DECIMAL(18,2) NOT NULL,           -- VAT paid on purchases
    net_vat         DECIMAL(18,2) NOT NULL,           -- output - input
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    submitted_at    TIMESTAMPTZ,
    rsge_reference  VARCHAR(100),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    UNIQUE(period_start, period_end)
);
```

### 3.8 Procurement

```sql
CREATE TABLE suppliers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code            VARCHAR(50) NOT NULL UNIQUE,
    name            VARCHAR(300) NOT NULL,
    name_ka         VARCHAR(300),
    tin             VARCHAR(20),                      -- RS.GE TIN
    is_vat_payer    BOOLEAN NOT NULL DEFAULT FALSE,
    contact_person  VARCHAR(200),
    phone           VARCHAR(50),
    email           VARCHAR(200),
    address         TEXT,
    payment_terms   INT NOT NULL DEFAULT 30,          -- days
    credit_limit    DECIMAL(18,2),
    rating          DECIMAL(3,2),                     -- 0.00 to 5.00
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    settings        JSONB NOT NULL DEFAULT '{}',
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE purchase_orders (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    po_number       VARCHAR(50) NOT NULL UNIQUE,
    supplier_id     UUID NOT NULL REFERENCES suppliers(id),
    warehouse_id    UUID NOT NULL REFERENCES warehouses(id),
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    -- DRAFT, PENDING_APPROVAL, APPROVED, SENT, PARTIALLY_RECEIVED, RECEIVED, CANCELLED
    order_date      DATE NOT NULL,
    expected_date   DATE,
    subtotal        DECIMAL(18,2) NOT NULL DEFAULT 0,
    vat_total       DECIMAL(18,2) NOT NULL DEFAULT 0,
    total           DECIMAL(18,2) NOT NULL DEFAULT 0,
    notes           TEXT,
    created_by      UUID NOT NULL REFERENCES users(id),
    approved_by     UUID REFERENCES users(id),
    approved_at     TIMESTAMPTZ,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE purchase_order_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    purchase_order_id UUID NOT NULL REFERENCES purchase_orders(id),
    line_number     INT NOT NULL,
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    ordered_qty     DECIMAL(18,3) NOT NULL,
    received_qty    DECIMAL(18,3) NOT NULL DEFAULT 0,
    unit_price      DECIMAL(18,2) NOT NULL,
    vat_amount      DECIMAL(18,2) NOT NULL,
    line_total      DECIMAL(18,2) NOT NULL
);

CREATE TABLE goods_receipt_notes (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    grn_number      VARCHAR(50) NOT NULL UNIQUE,
    purchase_order_id UUID NOT NULL REFERENCES purchase_orders(id),
    warehouse_id    UUID NOT NULL REFERENCES warehouses(id),
    supplier_id     UUID NOT NULL REFERENCES suppliers(id),
    rsge_waybill_id UUID,                            -- linked RS.GE waybill
    receipt_date    DATE NOT NULL,
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT',
    notes           TEXT,
    received_by     UUID NOT NULL REFERENCES users(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE goods_receipt_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    grn_id          UUID NOT NULL REFERENCES goods_receipt_notes(id),
    po_line_id      UUID NOT NULL REFERENCES purchase_order_lines(id),
    product_id      UUID NOT NULL REFERENCES products(id),
    variant_id      UUID REFERENCES product_variants(id),
    received_qty    DECIMAL(18,3) NOT NULL,
    accepted_qty    DECIMAL(18,3) NOT NULL,
    rejected_qty    DECIMAL(18,3) NOT NULL DEFAULT 0,
    batch_number    VARCHAR(100),
    expiry_date     DATE,
    unit_cost       DECIMAL(18,2) NOT NULL
);
```

### 3.9 Accounting & Finance

```sql
CREATE TABLE chart_of_accounts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_code    VARCHAR(20) NOT NULL UNIQUE,
    name            VARCHAR(200) NOT NULL,
    name_ka         VARCHAR(200),
    account_type    VARCHAR(30) NOT NULL,             -- ASSET, LIABILITY, EQUITY, REVENUE, EXPENSE
    parent_id       UUID REFERENCES chart_of_accounts(id),
    is_header       BOOLEAN NOT NULL DEFAULT FALSE,   -- group header, cannot post to
    is_system       BOOLEAN NOT NULL DEFAULT FALSE,   -- system accounts cannot be deleted
    balance_type    VARCHAR(10) NOT NULL,             -- DEBIT, CREDIT
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE journal_entries (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    entry_number    VARCHAR(50) NOT NULL UNIQUE,
    entry_date      DATE NOT NULL,
    description     VARCHAR(500) NOT NULL,
    source_type     VARCHAR(50) NOT NULL,             -- POS_SALE, PURCHASE, TRANSFER, ADJUSTMENT, MANUAL
    source_id       UUID,
    status          VARCHAR(20) NOT NULL DEFAULT 'DRAFT', -- DRAFT, POSTED, REVERSED
    total_debit     DECIMAL(18,2) NOT NULL,
    total_credit    DECIMAL(18,2) NOT NULL,
    posted_at       TIMESTAMPTZ,
    posted_by       UUID REFERENCES users(id),
    reversed_by_id  UUID REFERENCES journal_entries(id),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    created_by      UUID NOT NULL REFERENCES users(id),
    CONSTRAINT balanced_entry CHECK (total_debit = total_credit)
) PARTITION BY RANGE (entry_date);

CREATE TABLE journal_entry_lines (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    journal_entry_id UUID NOT NULL,                   -- references journal_entries (partitioned)
    line_number     INT NOT NULL,
    account_id      UUID NOT NULL REFERENCES chart_of_accounts(id),
    description     VARCHAR(500),
    debit_amount    DECIMAL(18,2) NOT NULL DEFAULT 0,
    credit_amount   DECIMAL(18,2) NOT NULL DEFAULT 0,
    CONSTRAINT single_side CHECK (
        (debit_amount > 0 AND credit_amount = 0) OR
        (debit_amount = 0 AND credit_amount > 0)
    )
);

CREATE TABLE bank_accounts (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    account_name    VARCHAR(200) NOT NULL,
    bank_name       VARCHAR(200) NOT NULL,
    account_number  VARCHAR(50) NOT NULL,
    iban            VARCHAR(50),
    currency        VARCHAR(3) NOT NULL DEFAULT 'GEL',
    gl_account_id   UUID NOT NULL REFERENCES chart_of_accounts(id),
    current_balance DECIMAL(18,2) NOT NULL DEFAULT 0,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

### 3.10 CRM

```sql
CREATE TABLE customers (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_number VARCHAR(50) NOT NULL UNIQUE,
    first_name      VARCHAR(100),
    last_name       VARCHAR(100),
    first_name_ka   VARCHAR(100),
    last_name_ka    VARCHAR(100),
    company_name    VARCHAR(300),
    tin             VARCHAR(20),
    phone           VARCHAR(50),
    email           VARCHAR(200),
    date_of_birth   DATE,
    gender          VARCHAR(10),
    loyalty_card_number VARCHAR(50) UNIQUE,
    loyalty_tier    VARCHAR(30) DEFAULT 'STANDARD',
    loyalty_points  INT NOT NULL DEFAULT 0,
    total_purchases DECIMAL(18,2) NOT NULL DEFAULT 0,
    total_visits    INT NOT NULL DEFAULT 0,
    last_visit_at   TIMESTAMPTZ,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    consent_sms     BOOLEAN NOT NULL DEFAULT FALSE,
    consent_email   BOOLEAN NOT NULL DEFAULT FALSE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE loyalty_transactions (
    id              UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    customer_id     UUID NOT NULL REFERENCES customers(id),
    transaction_type VARCHAR(20) NOT NULL,            -- EARN, REDEEM, ADJUST, EXPIRE
    points          INT NOT NULL,
    reference_type  VARCHAR(50),
    reference_id    UUID,
    description     VARCHAR(200),
    balance_after   INT NOT NULL,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);
```

---

## 4. Indexing Strategy

```sql
-- Product search
CREATE INDEX idx_products_sku ON products(sku);
CREATE INDEX idx_products_category ON products(category_id) WHERE is_active = TRUE;
CREATE INDEX idx_product_barcodes_barcode ON product_barcodes(barcode);

-- Inventory
CREATE INDEX idx_stock_levels_product_warehouse ON stock_levels(product_id, warehouse_id);
CREATE INDEX idx_stock_movements_product ON stock_movements(product_id, created_at DESC);
CREATE INDEX idx_stock_movements_reference ON stock_movements(reference_type, reference_id);

-- POS
CREATE INDEX idx_pos_transactions_store_date ON pos_transactions(store_id, created_at DESC);
CREATE INDEX idx_pos_transactions_session ON pos_transactions(session_id);
CREATE INDEX idx_pos_payments_method ON pos_payments(payment_method, created_at DESC);

-- Compliance
CREATE INDEX idx_fiscal_documents_status ON fiscal_documents(status) WHERE status IN ('PENDING', 'QUEUED', 'FAILED');
CREATE INDEX idx_fiscal_documents_deadline ON fiscal_documents(submission_deadline) WHERE status = 'PENDING';
CREATE INDEX idx_rsge_comm_log_correlation ON rsge_communication_log(correlation_id);
CREATE INDEX idx_rsge_comm_log_document ON rsge_communication_log(fiscal_document_id);

-- Finance
CREATE INDEX idx_journal_entries_date ON journal_entries(entry_date);
CREATE INDEX idx_journal_entry_lines_account ON journal_entry_lines(account_id);

-- CRM
CREATE INDEX idx_customers_phone ON customers(phone) WHERE phone IS NOT NULL;
CREATE INDEX idx_customers_loyalty_card ON customers(loyalty_card_number) WHERE loyalty_card_number IS NOT NULL;

-- Suppliers
CREATE INDEX idx_suppliers_tin ON suppliers(tin) WHERE tin IS NOT NULL;
```

---

## 5. Partitioning Strategy

```sql
-- Monthly partitions for high-volume tables
-- pos_transactions: by created_at (monthly)
CREATE TABLE pos_transactions_2026_01 PARTITION OF pos_transactions
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');
-- ... auto-created by partition management job

-- stock_movements: by created_at (monthly)
CREATE TABLE stock_movements_2026_01 PARTITION OF stock_movements
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

-- rsge_communication_log: by created_at (monthly)
CREATE TABLE rsge_comm_log_2026_01 PARTITION OF rsge_communication_log
    FOR VALUES FROM ('2026-01-01') TO ('2026-02-01');

-- journal_entries: by entry_date (yearly)
CREATE TABLE journal_entries_2026 PARTITION OF journal_entries
    FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');

-- Audit log (shared schema): by created_at (monthly)
-- Retained for 10 years, old partitions moved to archive tablespace
```

---

## 6. Data Retention Policy

| Data Type | Active Retention | Archive Retention | Total |
|-----------|-----------------|-------------------|-------|
| POS Transactions | 2 years | 8 years (archive tablespace) | 10 years |
| Stock Movements | 2 years | 8 years | 10 years |
| RS.GE Communication Log | 3 years | 7 years | 10 years |
| Fiscal Documents | 6 years (legal minimum) | 4 years | 10 years |
| Journal Entries | 6 years | 4 years | 10 years |
| Audit Log | 3 years | 7 years | 10 years |
| Customer Data | Active lifetime | Per GDPR deletion request | — |
