# Solution Architecture Document

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Draft — Awaiting Architecture Review

---

## 1. Architecture Overview

### 1.1 Architecture Style: Modular Monolith

**Decision**: Modular Monolith with Event-Driven internals, designed for future microservice extraction.

**Rationale**:
- **Transactional Consistency**: ERP operations (sale → inventory → accounting → compliance) require strong consistency. A monolith provides this naturally without distributed transactions.
- **Operational Simplicity**: A single deployment unit reduces DevOps complexity for a team scaling an ERP. Microservices introduce network partitioning, service discovery, and distributed debugging overhead that is premature for initial delivery.
- **RS.GE Integration**: SOAP-based RS.GE communication benefits from shared in-process state for retry logic, queue management, and transaction correlation.
- **Future Extraction Path**: Each module has clean boundaries (own database schema, defined API contracts, event-based communication). Any module can be extracted to an independent service when scaling demands it.

**Trade-offs**:
- Single deployment means all modules deploy together (mitigated by feature flags and blue-green deployment)
- Vertical scaling before horizontal (mitigated by stateless design and database partitioning)

### 1.2 High-Level Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│                        CLIENT LAYER                                  │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐   │
│  │ Web App  │  │ POS App  │  │ Mobile   │  │ External APIs    │   │
│  │ (React)  │  │ (React)  │  │ (Flutter)│  │ (Suppliers/Banks)│   │
│  └────┬─────┘  └────┬─────┘  └────┬─────┘  └────────┬─────────┘   │
│       │              │              │                  │             │
└───────┼──────────────┼──────────────┼──────────────────┼─────────────┘
        │              │              │                  │
┌───────▼──────────────▼──────────────▼──────────────────▼─────────────┐
│                      API GATEWAY (YARP)                               │
│  ┌─────────────┐ ┌──────────────┐ ┌────────────┐ ┌──────────────┐  │
│  │ Auth/JWT    │ │ Rate Limiting│ │ API Version│ │ Request Log  │  │
│  └─────────────┘ └──────────────┘ └────────────┘ └──────────────┘  │
└──────────────────────────┬───────────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────────────┐
│                    APPLICATION LAYER (.NET 9)                         │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    COMPLIANCE & TAX LAYER                        │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐  │ │
│  │  │ Waybill  │ │ Invoice  │ │ VAT      │ │ QES / Digital    │  │ │
│  │  │ Manager  │ │ Manager  │ │ Engine   │ │ Signature        │  │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘  │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐  │ │
│  │  │ RS.GE    │ │ Retry    │ │ Audit    │ │ Compliance Rule  │  │ │
│  │  │ SOAP     │ │ Queue    │ │ Logger   │ │ Engine           │  │ │
│  │  │ Client   │ │          │ │          │ │                  │  │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
│                                                                       │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│  │ POS      │ │Inventory │ │Warehouse │ │Procure-  │ │ Product  │ │
│  │ Module   │ │ Module   │ │ Module   │ │ment      │ │ Module   │ │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────┐ │
│  │ Pricing  │ │Accounting│ │ CRM      │ │ Supplier │ │Reporting │ │
│  │ Module   │ │ Module   │ │ Module   │ │ Module   │ │ Module   │ │
│  └──────────┘ └──────────┘ └──────────┘ └──────────┘ └──────────┘ │
│  ┌──────────┐ ┌──────────┐ ┌──────────┐                           │
│  │ Approval │ │Notifica- │ │ AI/BI    │                           │
│  │ Workflow │ │ tion     │ │ Module   │                           │
│  └──────────┘ └──────────┘ └──────────┘                           │
│                                                                       │
│  ┌─────────────────────────────────────────────────────────────────┐ │
│  │                    SHARED INFRASTRUCTURE                         │ │
│  │  ┌──────────┐ ┌──────────┐ ┌──────────┐ ┌──────────────────┐  │ │
│  │  │ Event    │ │ Identity │ │ Multi-   │ │ Background       │  │ │
│  │  │ Bus      │ │ & Access │ │ Tenancy  │ │ Job Scheduler    │  │ │
│  │  └──────────┘ └──────────┘ └──────────┘ └──────────────────┘  │ │
│  └─────────────────────────────────────────────────────────────────┘ │
└──────────────────────────┬───────────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────────────┐
│                      DATA LAYER                                       │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐    │
│  │PostgreSQL│  │ Redis    │  │ Elastic  │  │ RabbitMQ         │    │
│  │ (Primary)│  │ (Cache)  │  │ Search   │  │ (Message Queue)  │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
                           │
┌──────────────────────────▼───────────────────────────────────────────┐
│                   EXTERNAL INTEGRATIONS                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐    │
│  │ RS.GE    │  │ Bank of  │  │ TBC Bank │  │ Payment          │    │
│  │ (SOAP)   │  │ Georgia  │  │ API      │  │ Terminals        │    │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 2. Module Architecture

### 2.1 Module Boundaries and Responsibilities

Each module follows a consistent internal structure:

```
Module/
├── Domain/           # Entities, Value Objects, Domain Events
├── Application/      # Use Cases, DTOs, Validators
├── Infrastructure/   # Repository implementations, External service clients
└── API/              # Controllers, Request/Response models
```

### 2.2 Module Communication

**Synchronous (In-Process)**:
- Module-to-module calls via defined interfaces (no direct entity access)
- Used for queries and operations requiring immediate consistency

**Asynchronous (Event Bus)**:
- Domain events published to in-memory event bus (MediatR)
- Used for cross-cutting concerns: audit logging, compliance checks, notifications
- Critical events also published to RabbitMQ for durability

### 2.3 Module Dependency Rules

```
POS Module ──────────→ Product Module (read prices)
    │                → Inventory Module (check/deduct stock)
    │                → Pricing Module (apply discounts)
    │                → CRM Module (loyalty points)
    │                → Compliance Layer (fiscal receipt)
    │                → Accounting Module (journal entry)
    ▼
Compliance Layer ──→ RS.GE SOAP Client
    │              → Audit Logger
    │              → Retry Queue (RabbitMQ)
    ▼
Inventory Module ──→ Compliance Layer (waybill for transfers)
    │              → Warehouse Module (location management)
    │              → Accounting Module (cost tracking)
    ▼
Procurement Module → Supplier Module (vendor data)
    │              → Inventory Module (goods receipt)
    │              → Compliance Layer (purchase waybills)
    │              → Accounting Module (AP entries)
    │              → Approval Workflow (purchase approval)
```

---

## 3. Compliance & Tax Layer (Core Architecture)

### 3.1 Design Principles

1. **Every fiscal transaction MUST pass through this layer** — no bypass possible
2. **Queue-based RS.GE communication** — never block business operations on RS.GE availability
3. **Complete audit trail** — every request/response logged with correlation ID
4. **Retry with exponential backoff** — failed RS.GE calls automatically retried
5. **Configuration-driven rules** — tax rates, thresholds, deadlines configurable without code changes
6. **Idempotent operations** — safe to retry any RS.GE call

### 3.2 Compliance Layer Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                  COMPLIANCE & TAX LAYER                      │
│                                                               │
│  ┌─────────────┐    ┌─────────────────────────────────────┐ │
│  │ Transaction  │    │        COMPLIANCE PIPELINE           │ │
│  │ Interceptor  │───→│                                     │ │
│  │ (Middleware)  │    │  1. Validate Transaction            │ │
│  └─────────────┘    │  2. Determine Fiscal Requirements    │ │
│                      │  3. Generate Fiscal Documents        │ │
│                      │  4. Enqueue for RS.GE Submission     │ │
│                      │  5. Log to Audit Trail               │ │
│                      └──────────────┬──────────────────────┘ │
│                                     │                         │
│  ┌──────────────────────────────────▼──────────────────────┐ │
│  │              RS.GE COMMUNICATION QUEUE                   │ │
│  │                                                           │
│  │  ┌──────────┐  ┌──────────┐  ┌──────────────────────┐  │ │
│  │  │ Outbound │  │ Retry    │  │ Dead Letter Queue    │  │ │
│  │  │ Queue    │  │ Queue    │  │ (Manual Resolution)  │  │ │
│  │  └─────┬────┘  └─────┬────┘  └──────────────────────┘  │ │
│  │        │              │                                   │ │
│  │  ┌─────▼──────────────▼───┐                              │ │
│  │  │   RS.GE SOAP Client    │                              │ │
│  │  │  ┌──────────────────┐  │                              │ │
│  │  │  │ WayBill Service  │  │                              │ │
│  │  │  │ Invoice Service  │  │                              │ │
│  │  │  │ TIN Lookup       │  │                              │ │
│  │  │  │ VAT Verification │  │                              │ │
│  │  │  └──────────────────┘  │                              │ │
│  │  └────────────────────────┘                              │ │
│  └──────────────────────────────────────────────────────────┘ │
│                                                               │
│  ┌──────────────────────────────────────────────────────────┐ │
│  │                    AUDIT STORE                            │ │
│  │  Request/Response Logs │ Correlation IDs │ Timestamps     │ │
│  │  Immutable Event Log   │ Digital Signatures │ Retention   │ │
│  └──────────────────────────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────────┘
```

### 3.3 RS.GE Communication Flow

```
Business Transaction
    │
    ▼
Compliance Validator ──→ Is fiscal action required?
    │                         │ No → Continue
    │ Yes                     ▼
    ▼
Generate Fiscal Document (Waybill/Invoice)
    │
    ▼
Persist to Local DB (status: PENDING)
    │
    ▼
Publish to Outbound Queue (RabbitMQ)
    │
    ▼
RS.GE Worker picks up message
    │
    ├── Success → Update status: CONFIRMED, log response
    │
    ├── Transient Failure → Move to Retry Queue
    │   └── Retry with exponential backoff (1s, 2s, 4s, 8s, 16s, 32s, 60s, 300s)
    │   └── Max 10 retries over ~7 minutes, then:
    │
    └── Permanent Failure → Move to Dead Letter Queue
        └── Alert compliance officer
        └── Manual resolution required
```

### 3.4 VAT Engine

```
Sale Transaction
    │
    ▼
VAT Determination
    │
    ├── Is seller VAT-registered? ──→ No → Apply alternative tax regime
    │       │ Yes
    │       ▼
    ├── Is item VAT-exempt? ──→ Yes → Zero VAT, flag exemption reason
    │       │ No
    │       ▼
    ├── Is reverse charge applicable? ──→ Yes → Reverse charge accounting
    │       │ No
    │       ▼
    └── Apply 18% VAT
        │
        ▼
    Calculate: Net Amount, VAT Amount, Gross Amount
        │
        ▼
    Generate Invoice with VAT breakdown
        │
        ▼
    Queue for RS.GE upload (30-day deadline tracking)
```

---

## 4. Data Architecture

### 4.1 Multi-Tenancy Strategy

**Approach**: Schema-per-tenant with shared PostgreSQL cluster.

Each company (tenant) gets its own database schema:
- `company_001.*` — all tables for Company 1
- `company_002.*` — all tables for Company 2
- `shared.*` — shared reference data, system configuration

**Rationale**:
- Strong data isolation without separate database instances
- Simplified backup/restore per company
- Cross-company reporting via schema-qualified queries
- Lower infrastructure cost than database-per-tenant

### 4.2 Database Schema Organization

```
PostgreSQL Cluster
│
├── shared schema
│   ├── companies
│   ├── system_config
│   ├── rs_ge_reference_data (units, transport types, waybill types)
│   └── audit_log (immutable, partitioned by month)
│
├── company_{id} schema (per tenant)
│   ├── Core
│   │   ├── stores
│   │   ├── warehouses
│   │   ├── users
│   │   ├── roles
│   │   └── permissions
│   │
│   ├── Product
│   │   ├── categories
│   │   ├── products
│   │   ├── product_variants
│   │   ├── product_barcodes
│   │   ├── product_images
│   │   └── product_specifications
│   │
│   ├── Pricing
│   │   ├── price_lists
│   │   ├── price_list_items
│   │   ├── promotions
│   │   ├── discount_rules
│   │   └── loyalty_tiers
│   │
│   ├── Inventory
│   │   ├── stock_levels (partitioned by location)
│   │   ├── stock_movements
│   │   ├── stock_adjustments
│   │   ├── stock_counts
│   │   ├── batch_tracking
│   │   ├── serial_numbers
│   │   └── expiration_tracking
│   │
│   ├── Warehouse
│   │   ├── warehouse_locations
│   │   ├── receiving_orders
│   │   ├── shipping_orders
│   │   ├── transfer_orders
│   │   ├── pick_lists
│   │   └── pack_lists
│   │
│   ├── Procurement
│   │   ├── purchase_requisitions
│   │   ├── purchase_orders
│   │   ├── purchase_order_lines
│   │   ├── goods_receipt_notes
│   │   ├── supplier_quotations
│   │   └── supplier_contracts
│   │
│   ├── POS
│   │   ├── pos_sessions
│   │   ├── pos_transactions (partitioned by date)
│   │   ├── pos_transaction_lines
│   │   ├── pos_payments
│   │   ├── pos_returns
│   │   └── daily_closings
│   │
│   ├── Compliance
│   │   ├── fiscal_documents
│   │   ├── rsge_waybills
│   │   ├── rsge_invoices
│   │   ├── rsge_communication_log (partitioned by month)
│   │   ├── vat_declarations
│   │   └── compliance_queue
│   │
│   ├── Finance
│   │   ├── chart_of_accounts
│   │   ├── journal_entries (partitioned by fiscal year)
│   │   ├── journal_entry_lines
│   │   ├── accounts_receivable
│   │   ├── accounts_payable
│   │   ├── bank_accounts
│   │   ├── bank_transactions
│   │   └── payment_schedules
│   │
│   ├── CRM
│   │   ├── customers
│   │   ├── customer_addresses
│   │   ├── loyalty_accounts
│   │   ├── loyalty_transactions
│   │   └── customer_segments
│   │
│   └── Supplier
│       ├── suppliers
│       ├── supplier_contacts
│       ├── supplier_performance
│       └── supplier_balances
│
└── Partitioning Strategy
    ├── pos_transactions → Range partitioned by month
    ├── stock_movements → Range partitioned by month
    ├── journal_entries → Range partitioned by fiscal year
    ├── rsge_communication_log → Range partitioned by month
    └── audit_log → Range partitioned by month
```

### 4.3 Key Data Design Decisions

1. **All monetary values stored as `DECIMAL(18,2)`** — never floating point
2. **All timestamps in UTC** with timezone-aware columns (`TIMESTAMPTZ`)
3. **Soft deletes** for all business entities (deleted_at column)
4. **Optimistic concurrency** via row version columns
5. **Natural + surrogate keys**: UUID primary keys, natural keys as unique indexes
6. **Audit columns on every table**: created_at, created_by, updated_at, updated_by
7. **Event sourcing for compliance**: fiscal document state changes stored as immutable events
8. **Table partitioning** for high-volume tables (transactions, stock movements, audit logs)

---

## 5. API Architecture

### 5.1 API Design

- **Style**: RESTful JSON API
- **Versioning**: URL-based (`/api/v1/...`)
- **Authentication**: JWT Bearer tokens (access + refresh token pair)
- **Authorization**: Role-based (RBAC) with per-endpoint permission checks
- **Pagination**: Cursor-based for large datasets, offset-based for admin views
- **Filtering**: OData-style query parameters
- **Rate Limiting**: Per-tenant, per-user limits via Redis

### 5.2 API Module Structure

```
/api/v1/
├── /auth
│   ├── POST /login
│   ├── POST /refresh
│   ├── POST /logout
│   └── POST /2fa/verify
│
├── /products
│   ├── GET    /                    (list, paginated)
│   ├── POST   /                    (create)
│   ├── GET    /{id}                (get by ID)
│   ├── PUT    /{id}                (update)
│   ├── DELETE /{id}                (soft delete)
│   ├── GET    /{id}/variants
│   ├── GET    /{id}/barcodes
│   └── GET    /search?q=           (Elasticsearch)
│
├── /inventory
│   ├── GET    /stock-levels        (by location)
│   ├── POST   /adjustments         (stock adjustment)
│   ├── POST   /transfers           (inter-location transfer)
│   ├── POST   /counts              (stock count session)
│   └── GET    /movements           (movement history)
│
├── /pos
│   ├── POST   /sessions/open       (open register)
│   ├── POST   /sessions/close      (daily closing)
│   ├── POST   /transactions        (create sale)
│   ├── POST   /transactions/{id}/void
│   ├── POST   /returns             (return transaction)
│   └── GET    /transactions        (transaction history)
│
├── /procurement
│   ├── CRUD   /requisitions
│   ├── CRUD   /purchase-orders
│   ├── POST   /goods-receipts
│   └── CRUD   /supplier-quotations
│
├── /warehouse
│   ├── CRUD   /receiving-orders
│   ├── CRUD   /shipping-orders
│   ├── CRUD   /transfer-orders
│   └── GET    /locations
│
├── /compliance
│   ├── GET    /waybills             (list waybills)
│   ├── GET    /waybills/{id}        (waybill detail)
│   ├── GET    /invoices             (list fiscal invoices)
│   ├── GET    /queue                (pending RS.GE submissions)
│   ├── POST   /queue/{id}/retry     (manual retry)
│   ├── GET    /vat-summary          (VAT calculation summary)
│   └── GET    /audit-log            (compliance audit trail)
│
├── /finance
│   ├── GET    /chart-of-accounts
│   ├── CRUD   /journal-entries
│   ├── GET    /accounts-receivable
│   ├── GET    /accounts-payable
│   ├── GET    /bank-reconciliation
│   └── GET    /reports/{type}       (P&L, Balance Sheet, etc.)
│
├── /crm
│   ├── CRUD   /customers
│   ├── GET    /customers/{id}/history
│   ├── CRUD   /loyalty
│   └── CRUD   /segments
│
├── /suppliers
│   ├── CRUD   /
│   ├── GET    /{id}/performance
│   └── GET    /{id}/balance
│
├── /reports
│   ├── GET    /sales
│   ├── GET    /inventory
│   ├── GET    /financial
│   ├── GET    /vat
│   └── POST   /export              (Excel/PDF/CSV)
│
├── /admin
│   ├── CRUD   /users
│   ├── CRUD   /roles
│   ├── CRUD   /stores
│   ├── CRUD   /warehouses
│   └── GET    /system-health
│
└── /notifications
    ├── GET    /                     (user notifications)
    ├── PUT    /{id}/read
    └── GET    /settings
```

---

## 6. Security Architecture

### 6.1 Authentication Flow

```
User Login
    │
    ├── Username/Password validation
    │
    ├── 2FA check (if enabled)
    │   ├── TOTP (Authenticator App)
    │   └── SMS code
    │
    ├── Device verification
    │   └── New device → additional verification
    │
    ├── IP restriction check
    │
    └── Token generation
        ├── Access Token (JWT, 15 min expiry)
        │   Claims: user_id, company_id, store_id, roles[], permissions[]
        └── Refresh Token (opaque, 7 day expiry, stored in DB)
```

### 6.2 Authorization Model

```
Company (Tenant)
  └── Role (e.g., Store Manager, Cashier, Accountant)
       └── Permission Set
            ├── Module Access (POS, Inventory, Finance, etc.)
            ├── Action (Read, Create, Update, Delete, Approve)
            └── Scope (Own Store, Own Region, All Stores)
```

**Built-in Roles**:
| Role | Scope | Key Permissions |
|------|-------|----------------|
| System Admin | Global | All permissions |
| Company Admin | Company | Company-wide management |
| Store Manager | Store | Store operations, reporting |
| Cashier | POS Terminal | POS transactions only |
| Warehouse Operator | Warehouse | Receiving, dispatch, stock count |
| Procurement Officer | Company | Purchase orders, supplier management |
| Accountant | Company | Finance, reporting, compliance |
| Compliance Officer | Company | RS.GE management, audit trails |
| Regional Manager | Region | Multi-store oversight, approvals |
| Executive | Company | Read-only dashboards, all reports |

### 6.3 Data Protection

| Layer | Mechanism |
|-------|-----------|
| In Transit | TLS 1.3 for all HTTP, TLS for database connections |
| At Rest | AES-256 encryption for sensitive fields (SSN, bank details) |
| Database | Row-Level Security (RLS) for multi-tenant isolation |
| Secrets | HashiCorp Vault or AWS Secrets Manager |
| PII | Masked in logs, encrypted in storage |
| Backup | Encrypted backups with separate key management |

### 6.4 Audit Logging

Every state-changing operation generates an immutable audit entry:

```json
{
  "id": "uuid",
  "timestamp": "2026-06-18T14:30:00Z",
  "company_id": "uuid",
  "user_id": "uuid",
  "action": "POS_SALE_COMPLETED",
  "entity_type": "pos_transaction",
  "entity_id": "uuid",
  "old_values": null,
  "new_values": { "total": 150.00, "vat": 27.00 },
  "ip_address": "192.168.1.100",
  "device_id": "POS-STORE01-REG03",
  "correlation_id": "uuid",
  "metadata": { "store_id": "uuid", "session_id": "uuid" }
}
```

Audit logs are:
- Append-only (no updates, no deletes)
- Partitioned by month
- Retained for 10 years
- Backed up independently

---

## 7. Offline Architecture (POS)

### 7.1 Offline-First Design

```
┌─────────────────────────────────────┐
│           POS TERMINAL               │
│                                       │
│  ┌─────────────┐  ┌──────────────┐  │
│  │ React POS   │  │ Local SQLite │  │
│  │ Application │──│ Database     │  │
│  └──────┬──────┘  └──────────────┘  │
│         │                             │
│  ┌──────▼──────────────────────────┐ │
│  │      Sync Engine                 │ │
│  │  ┌────────┐  ┌───────────────┐  │ │
│  │  │Outbound│  │  Inbound      │  │ │
│  │  │Queue   │  │  Queue        │  │ │
│  │  └────────┘  └───────────────┘  │ │
│  └──────┬──────────────────────────┘ │
│         │ Online?                     │
└─────────┼─────────────────────────────┘
          │
          ▼
┌─────────────────────────────────────┐
│        CENTRAL SERVER                │
│  Sync API → Conflict Resolution     │
│  → Apply Changes → Broadcast Updates│
└─────────────────────────────────────┘
```

### 7.2 Offline Capabilities

| Feature | Offline Support | Notes |
|---------|----------------|-------|
| POS Transactions | Full | All sales stored locally |
| Product Catalog | Read-only | Synced daily or on-demand |
| Price Lists | Read-only | Synced before shift |
| Stock Levels | Approximate | Deducted locally, reconciled on sync |
| Fiscal Receipts | Queued | Generated locally, uploaded on sync |
| Waybills | Not available | Requires RS.GE connectivity |
| Reports | Limited | Local data only |

### 7.3 Sync Conflict Resolution

| Conflict Type | Strategy |
|---------------|----------|
| Sale transaction | Always accept (append-only, server assigns final ID) |
| Stock level | Server recalculates from all terminal transactions |
| Price change | Server wins (latest server price takes precedence) |
| Customer update | Last-write-wins with merge for non-conflicting fields |
| Product update | Server wins |

---

## 8. Infrastructure Architecture

### 8.1 Deployment Architecture

```
┌─────────────────────────────────────────────────────┐
│                   KUBERNETES CLUSTER                  │
│                                                       │
│  ┌──────────────────────────────────────────────┐    │
│  │  Namespace: erp-production                    │    │
│  │                                                │    │
│  │  ┌──────┐  ┌──────┐  ┌──────┐  ┌──────────┐ │    │
│  │  │ API  │  │ API  │  │ API  │  │ API GW   │ │    │
│  │  │ Pod 1│  │ Pod 2│  │ Pod 3│  │ (YARP)   │ │    │
│  │  └──────┘  └──────┘  └──────┘  └──────────┘ │    │
│  │                                                │    │
│  │  ┌──────┐  ┌──────┐  ┌──────────────────────┐│    │
│  │  │Worker│  │Worker│  │ RS.GE Worker (1 pod) ││    │
│  │  │ Pod 1│  │ Pod 2│  │ (serialized SOAP)    ││    │
│  │  └──────┘  └──────┘  └──────────────────────┘│    │
│  │                                                │    │
│  │  ┌──────────────────────────────────────────┐ │    │
│  │  │ Scheduled Jobs (CronJob)                  │ │    │
│  │  │ - Daily VAT summary                       │ │    │
│  │  │ - RS.GE reference data sync               │ │    │
│  │  │ - Invoice deadline monitoring              │ │    │
│  │  │ - Stock recalculation                      │ │    │
│  │  └──────────────────────────────────────────┘ │    │
│  └──────────────────────────────────────────────┘    │
│                                                       │
│  ┌──────────────────────────────────────────────┐    │
│  │  Namespace: erp-data                          │    │
│  │  ┌──────────────┐  ┌──────────────────────┐  │    │
│  │  │ PostgreSQL   │  │ PostgreSQL Replica    │  │    │
│  │  │ Primary      │  │ (read replicas × 2)  │  │    │
│  │  └──────────────┘  └──────────────────────┘  │    │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────┐ │    │
│  │  │ Redis    │  │ RabbitMQ │  │Elasticsearch│ │    │
│  │  │ Cluster  │  │ Cluster  │  │ Cluster     │ │    │
│  │  └──────────┘  └──────────┘  └────────────┘ │    │
│  └──────────────────────────────────────────────┘    │
│                                                       │
│  ┌──────────────────────────────────────────────┐    │
│  │  Namespace: erp-monitoring                    │    │
│  │  ┌──────────┐  ┌──────────┐  ┌────────────┐ │    │
│  │  │Prometheus│  │ Grafana  │  │ Seq / ELK  │ │    │
│  │  └──────────┘  └──────────┘  └────────────┘ │    │
│  └──────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────┘
```

### 8.2 Environment Strategy

| Environment | Purpose | Infrastructure |
|-------------|---------|---------------|
| Development | Active development | Docker Compose (local) |
| Staging | Integration testing, UAT | Single-node K8s |
| Production | Live operations | Multi-node K8s cluster |
| DR | Disaster recovery | Cross-region standby |

### 8.3 Scalability Strategy

| Component | Horizontal Scaling | Vertical Scaling |
|-----------|-------------------|-----------------|
| API Servers | Auto-scale 3-10 pods | 2 CPU / 4GB RAM per pod |
| Background Workers | Scale to queue depth | 1 CPU / 2GB RAM per pod |
| RS.GE Worker | Single pod (serialized) | Dedicated resources |
| PostgreSQL | Read replicas | Scale up primary |
| Redis | Cluster mode | Memory scaling |
| Elasticsearch | Add data nodes | Memory scaling |
| RabbitMQ | Cluster (3 nodes) | — |

**RS.GE Worker is intentionally single-instance**: RS.GE API calls must be serialized per service user to avoid race conditions and duplicate submissions. The queue ensures ordering.

---

## 9. Integration Architecture

### 9.1 Integration Patterns

```
┌──────────────────────────────────────────────────────┐
│              INTEGRATION LAYER                        │
│                                                        │
│  ┌────────────────────────────────────────────────┐   │
│  │           Adapter Pattern                       │   │
│  │  Each external system gets an adapter that:     │   │
│  │  - Translates internal DTOs to external format  │   │
│  │  - Handles authentication                       │   │
│  │  - Implements retry logic                       │   │
│  │  - Logs all communication                       │   │
│  │  - Provides health check                        │   │
│  └────────────────────────────────────────────────┘   │
│                                                        │
│  ┌────────────┐ ┌────────────┐ ┌────────────────────┐│
│  │ RS.GE      │ │ BoG iPay   │ │ TBC Pay           ││
│  │ Adapter    │ │ Adapter    │ │ Adapter            ││
│  │ (SOAP/XML) │ │ (REST/JSON)│ │ (REST/JSON)       ││
│  └────────────┘ └────────────┘ └────────────────────┘│
│  ┌────────────┐ ┌────────────┐ ┌────────────────────┐│
│  │ Payment    │ │ Receipt    │ │ Barcode Scanner    ││
│  │ Terminal   │ │ Printer    │ │ Adapter            ││
│  │ Adapter    │ │ Adapter    │ │                    ││
│  └────────────┘ └────────────┘ └────────────────────┘│
└──────────────────────────────────────────────────────┘
```

### 9.2 Bank of Georgia Integration

| Feature | Endpoint | Method |
|---------|----------|--------|
| Payment Processing | iPay API | REST |
| Installment Payments | iPay Installment API | REST |
| Account Information | Open Banking AIS API | REST |
| Payment Initiation | Open Banking PIS API | REST |
| Documentation | https://api.bog.ge/docs/en/ | — |

### 9.3 TBC Bank Integration

| Feature | Endpoint | Method |
|---------|----------|--------|
| E-commerce Checkout | `/tpay/checkout` | REST |
| QR Code Payments | `/tpay/qr` | REST |
| Recurring Payments | `/tpay/recurring` | REST |
| Pre-authorization | `/tpay/preauth` | REST |
| Access Token | `/tpay/access-token` | POST (client_id/client_secret) |
| Base URL (Production) | https://api.tbcbank.ge/v1 | — |

---

## 10. Monitoring & Observability

### 10.1 Monitoring Stack

| Component | Tool | Purpose |
|-----------|------|---------|
| Metrics | Prometheus + Grafana | System and business metrics |
| Logging | Seq or ELK Stack | Centralized log aggregation |
| Tracing | OpenTelemetry + Jaeger | Distributed request tracing |
| Alerting | Prometheus AlertManager | Incident notification |
| Health Checks | ASP.NET Health Checks | Service liveness/readiness |
| Uptime | StatusPage | External availability monitoring |

### 10.2 Critical Alerts

| Alert | Condition | Severity |
|-------|-----------|----------|
| RS.GE Queue Depth | > 100 pending items | Warning |
| RS.GE Queue Depth | > 500 pending items | Critical |
| RS.GE Dead Letter | Any item in DLQ | Critical |
| Invoice Deadline | Invoice not uploaded > 25 days | Warning |
| Invoice Deadline | Invoice not uploaded > 28 days | Critical |
| POS Offline | Terminal offline > 4 hours | Warning |
| Database Replication Lag | > 30 seconds | Warning |
| Disk Usage | > 80% | Warning |
| API Error Rate | > 5% of requests | Warning |
| API Latency (p99) | > 5 seconds | Warning |

---

## 11. Disaster Recovery

### 11.1 Recovery Objectives

| Metric | Target |
|--------|--------|
| RTO (Recovery Time Objective) | < 1 hour |
| RPO (Recovery Point Objective) | < 5 minutes |
| Backup Frequency | Continuous WAL shipping + daily full backup |
| Backup Retention | 30 days online, 1 year archive |
| DR Test Frequency | Quarterly |

### 11.2 Backup Strategy

```
PostgreSQL Primary
    │
    ├── WAL Streaming → Standby Replica (same region)
    ├── WAL Archiving → Object Storage (cross-region)
    ├── Daily pg_dump → Object Storage (encrypted)
    └── Monthly → Long-term archive (10 year retention for compliance)

Redis
    ├── RDB snapshots → Object Storage (hourly)
    └── AOF → Replica

RabbitMQ
    ├── Mirrored queues (3 nodes)
    └── Definition export → Object Storage (daily)
```

---

## 12. Technology Stack Summary

| Layer | Technology | Version | Justification |
|-------|-----------|---------|---------------|
| Backend Runtime | .NET 9 | 9.x | SOAP support for RS.GE, enterprise performance, strong typing |
| Web Framework | ASP.NET Core | 9.x | Mature, high-performance, built-in DI |
| API Gateway | YARP | Latest | .NET-native reverse proxy, flexible routing |
| ORM | Entity Framework Core | 9.x | Migration management, LINQ queries |
| CQRS/Events | MediatR | 12.x | In-process command/query/event mediation |
| Validation | FluentValidation | 11.x | Expressive validation rules |
| Database | PostgreSQL | 17 | Partitioning, JSON support, proven at scale |
| Cache | Redis | 7.x | Session, reference data, rate limiting |
| Search | Elasticsearch | 8.x | Full-text product search at scale |
| Message Queue | RabbitMQ | 4.x | Reliable RS.GE communication queue |
| Frontend | React | 19 | Component ecosystem, TypeScript support |
| UI Framework | Ant Design | 5.x | Enterprise-grade components, i18n |
| State Management | TanStack Query | 5.x | Server state management, caching |
| Mobile | Flutter | 3.x | Cross-platform, offline support |
| Containerization | Docker | Latest | Consistent environments |
| Orchestration | Kubernetes | 1.30+ | Production orchestration, auto-scaling |
| CI/CD | GitHub Actions | — | Automated build, test, deploy |
| Monitoring | Prometheus + Grafana | — | Metrics and dashboards |
| Logging | Seq | Latest | Structured logging for .NET |
| Cloud | AWS or Azure | — | Managed services, Georgian region proximity |
