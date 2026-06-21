# Data Retention Policy

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 2026
**Classification:** Internal -- Restricted
**Owner:** Platform Operations Team / Legal & Compliance
**Review Cycle:** Annually, or when regulations change

---

## 1. Purpose

This document defines the data retention, archival, and deletion policies for the Georgia Enterprise Retail ERP Platform. It addresses:

- Georgian tax law requirements (Revenue Service / RS.GE)
- Financial records retention obligations
- Transaction log management
- Audit trail preservation
- Customer data protection (GDPR considerations)
- Technical implementation of archival and purging

---

## 2. Regulatory Framework

### 2.1 Georgian Tax Code Requirements

The Georgian Tax Code (consolidated) and Revenue Service regulations establish the following mandatory retention periods:

| Requirement | Retention Period | Legal Basis |
|-------------|-----------------|-------------|
| Tax invoices and supporting documents | **6 years** from the end of the tax period | Georgian Tax Code, Article 73 |
| VAT records and calculations | **6 years** | Georgian Tax Code, VAT Chapter |
| Waybill records (RS.GE) | **6 years** | RS.GE regulatory requirements |
| Financial statements | **6 years** minimum | Georgian Tax Code |
| Employment and payroll records | **6 years** after employment ends | Labor Code of Georgia |
| Electronic fiscal documents | **6 years** from issuance | RS.GE electronic services regulations |

**Critical:** RS.GE compliance records must be preserved in their original electronic form. Printed copies alone are insufficient. The system must maintain the original XML payloads, submission timestamps, and RS.GE confirmation responses.

### 2.2 Georgian Accounting Standards

Under Georgian National Accounting Standards (GNAS) and alignment with IFRS:

| Record Type | Retention Period |
|-------------|-----------------|
| General ledger and journal entries | 6 years |
| Bank statements and reconciliations | 6 years |
| Accounts receivable/payable records | 6 years |
| Fixed asset registers | Life of asset + 6 years |
| Contracts and agreements | Duration + 6 years |

### 2.3 GDPR and Data Protection

Georgia's Law on Personal Data Protection (aligned with EU GDPR principles) requires:

- **Purpose limitation:** Personal data collected for a specific purpose must not be retained beyond that purpose
- **Data minimization:** Only necessary personal data should be retained
- **Right to erasure:** Individuals can request deletion of their personal data, subject to legal retention requirements
- **Lawful basis override:** Legal retention obligations (tax, financial) override individual deletion requests for the mandatory retention period

---

## 3. Data Classification and Retention Schedule

### 3.1 Tier 1: Regulatory-Mandatory (6+ Years)

These records MUST be retained for the full regulatory period. Deletion before the retention period expires is a compliance violation.

| Data Category | Database Table(s) | Retention | Archive After | Notes |
|--------------|-------------------|-----------|---------------|-------|
| RS.GE invoices | `invoices`, `invoice_items`, `rsge_submissions` | 6 years from issue date | 2 years | Include XML payloads and RS.GE responses |
| RS.GE waybills | `waybills`, `waybill_items`, `rsge_submissions` | 6 years from issue date | 2 years | Include confirmation numbers |
| VAT calculations | `vat_records`, `vat_returns` | 6 years from tax period end | 2 years | Linked to invoices |
| Sales orders (fiscal) | `sales_orders`, `sales_order_items` | 6 years from transaction date | 2 years | Financial records |
| Payment records | `payments`, `payment_transactions` | 6 years from payment date | 2 years | Bank reconciliation evidence |
| Audit trail (financial) | `audit_logs` (financial operations) | 6 years from event date | 3 years | Immutable, append-only |
| General ledger entries | `journal_entries`, `ledger_accounts` | 6 years from fiscal year end | 3 years | Core accounting |

### 3.2 Tier 2: Business-Critical (2-6 Years)

| Data Category | Database Table(s) | Retention | Archive After | Notes |
|--------------|-------------------|-----------|---------------|-------|
| Customer records | `customers`, `customer_addresses` | Duration of relationship + 3 years | 2 years after last transaction | Subject to GDPR deletion requests after retention period |
| Supplier records | `suppliers`, `supplier_contacts` | Duration of relationship + 3 years | 2 years after last transaction | |
| Product catalog (historical) | `products`, `product_prices` | 6 years (price history for audit) | 2 years | Price at time of sale must be reconstructable |
| Inventory movements | `inventory_movements`, `stock_adjustments` | 3 years | 1 year | For audit trail |
| Purchase orders | `purchase_orders`, `purchase_order_items` | 6 years | 2 years | Financial records |

### 3.3 Tier 3: Operational (30 Days - 2 Years)

| Data Category | Database Table(s) | Retention | Archive After | Notes |
|--------------|-------------------|-----------|---------------|-------|
| POS transaction logs | `pos_transactions`, `pos_receipts` | 2 years | 6 months | Detailed receipt data |
| User session logs | `user_sessions` | 90 days | N/A -- purge | Security monitoring |
| API request logs | `api_logs` | 90 days | N/A -- purge | Debugging, performance |
| RabbitMQ dead-letter messages | `dlq_messages` | 90 days | N/A -- purge | Failed message analysis |
| System health metrics | `health_metrics` | 30 days | N/A -- purge | Monitoring data |
| Temporary calculation caches | Redis keys | 24 hours | N/A -- auto-expire | TTL-managed |

### 3.4 Tier 4: Transient (Immediate - 30 Days)

| Data Category | Storage | Retention | Notes |
|--------------|---------|-----------|-------|
| User session tokens | Redis | Session duration + 1 hour | Auto-expire via TTL |
| Rate limiting counters | Redis | Window duration | Auto-expire |
| Email/notification queue | RabbitMQ | Until processed | Consumed and acknowledged |
| File upload temp storage | Filesystem | 24 hours | Cron cleanup |

---

## 4. Audit Trail Preservation

### 4.1 Audit Log Requirements

The audit trail is the cornerstone of regulatory compliance. The following rules apply:

1. **Immutability:** Audit log records MUST NEVER be updated or deleted from the active database. They are append-only.
2. **Completeness:** Every create, update, and delete operation on Tier 1 and Tier 2 data must generate an audit log entry.
3. **Attribution:** Every audit entry must record the user ID, timestamp (UTC), client IP, and the operation performed.
4. **Before/After:** For update operations, both the previous and new values must be recorded.
5. **Retention:** Audit logs follow the same retention period as the data they audit (minimum 6 years for financial data).

### 4.2 Audit Log Schema

```sql
-- Audit log entries are NEVER deleted from this table during the retention period.
-- Archival moves records to audit_logs_archive with identical schema.

CREATE TABLE audit_logs (
    id              BIGSERIAL PRIMARY KEY,
    entity_type     VARCHAR(100) NOT NULL,     -- e.g., 'Invoice', 'SalesOrder'
    entity_id       VARCHAR(100) NOT NULL,     -- Primary key of the affected record
    action          VARCHAR(20) NOT NULL,      -- 'CREATE', 'UPDATE', 'DELETE'
    user_id         UUID,                      -- Who performed the action
    user_name       VARCHAR(200),              -- Username at time of action
    client_ip       INET,                      -- Client IP address
    timestamp_utc   TIMESTAMPTZ NOT NULL DEFAULT now(),
    old_values      JSONB,                     -- Previous state (for UPDATE/DELETE)
    new_values      JSONB,                     -- New state (for CREATE/UPDATE)
    metadata        JSONB,                     -- Additional context
    checksum        VARCHAR(64)                -- SHA256 of the record for tamper detection
);

-- Partition by year for efficient archival
-- CREATE TABLE audit_logs_2026 PARTITION OF audit_logs
--     FOR VALUES FROM ('2026-01-01') TO ('2027-01-01');
```

### 4.3 Tamper Detection

Each audit log entry includes a SHA256 checksum computed from:
```
checksum = SHA256(entity_type + entity_id + action + user_id + timestamp_utc + old_values + new_values)
```

A periodic integrity check job should verify checksums have not been altered:
```sql
-- Run weekly to detect any tampering
SELECT id, entity_type, entity_id, timestamp_utc
FROM audit_logs
WHERE checksum != encode(
    sha256(
        (entity_type || entity_id || action || COALESCE(user_id::text,'') ||
         timestamp_utc::text || COALESCE(old_values::text,'') ||
         COALESCE(new_values::text,''))::bytea
    ), 'hex'
);
```

---

## 5. Data Archival Strategy

### 5.1 Archive Architecture

Data archival moves older records from active tables to archive tables within the same database. This keeps the active tables small for performance while maintaining regulatory access to historical data.

```
Active Tables                    Archive Tables
(hot data, fast queries)         (cold data, compliance queries)
┌─────────────────────┐         ┌─────────────────────────┐
│ invoices            │ ──2yr──>│ invoices_archive        │
│ sales_orders        │ ──2yr──>│ sales_orders_archive    │
│ audit_logs          │ ──3yr──>│ audit_logs_archive      │
│ pos_transactions    │ ──6mo──>│ pos_transactions_archive│
└─────────────────────┘         └─────────────────────────┘
                                         │
                                    After 6 years
                                         │
                                         ▼
                                ┌─────────────────────┐
                                │ Secure deletion      │
                                │ (with audit record)  │
                                └─────────────────────┘
```

### 5.2 Archive Table Naming Convention

- Archive tables use the suffix `_archive`: `invoices` -> `invoices_archive`
- Archive tables have identical schema to source tables
- Archive tables include an additional `archived_at TIMESTAMPTZ` column
- Archive tables are indexed on date columns for compliance queries

### 5.3 Archival Procedure

```sql
-- Example: Archive invoices older than 2 years
-- Run as a scheduled job (monthly on the 1st, outside business hours)

BEGIN;

-- Move records to archive
INSERT INTO invoices_archive (SELECT *, now() AS archived_at FROM invoices
    WHERE issue_date < now() - INTERVAL '2 years'
    AND id NOT IN (SELECT id FROM invoices_archive));

-- Verify record counts match
DO $$
DECLARE
    source_count INT;
    archive_count INT;
BEGIN
    SELECT count(*) INTO source_count FROM invoices
        WHERE issue_date < now() - INTERVAL '2 years';
    SELECT count(*) INTO archive_count FROM invoices_archive
        WHERE issue_date < now() - INTERVAL '2 years';

    IF source_count > archive_count THEN
        RAISE EXCEPTION 'Archive count mismatch: source=%, archive=%',
            source_count, archive_count;
    END IF;
END $$;

-- Remove from active table (only after successful archive)
DELETE FROM invoices WHERE issue_date < now() - INTERVAL '2 years'
    AND id IN (SELECT id FROM invoices_archive);

-- Log the archival operation
INSERT INTO audit_logs (entity_type, action, metadata, timestamp_utc)
VALUES ('invoices', 'ARCHIVE',
    jsonb_build_object('records_archived', (SELECT count(*) FROM invoices_archive
        WHERE archived_at > now() - INTERVAL '1 hour')),
    now());

COMMIT;

-- Update statistics
ANALYZE invoices;
ANALYZE invoices_archive;
```

### 5.4 Archive Query Access

Application code should support querying archive tables for compliance and audit purposes:

```sql
-- View to combine active and archived invoices for compliance queries
CREATE OR REPLACE VIEW invoices_all AS
    SELECT *, FALSE AS is_archived FROM invoices
    UNION ALL
    SELECT *, TRUE AS is_archived FROM invoices_archive;
```

---

## 6. Data Deletion (Purging)

### 6.1 Deletion Rules

| Condition | Action | Approval Required |
|-----------|--------|-------------------|
| Tier 3/4 data past retention | Automatic purge | None (automated) |
| Tier 2 data past retention | Manual purge after review | Operations lead |
| Tier 1 data past 6-year retention | Manual purge after legal review | Legal + Operations |
| GDPR erasure request (non-financial) | Delete within 30 days | Data Protection Officer |
| GDPR erasure request (financial data) | Anonymize after retention period | Legal + DPO |

### 6.2 Automated Purge Jobs

```sql
-- Daily purge of expired operational data
-- Schedule: 3:00 AM daily

-- Purge expired user sessions
DELETE FROM user_sessions WHERE expires_at < now() - INTERVAL '1 day';

-- Purge old API logs
DELETE FROM api_logs WHERE created_at < now() - INTERVAL '90 days';

-- Purge old health metrics
DELETE FROM health_metrics WHERE recorded_at < now() - INTERVAL '30 days';

-- Purge old dead-letter messages
DELETE FROM dlq_messages WHERE created_at < now() - INTERVAL '90 days';

-- Log purge operation
INSERT INTO audit_logs (entity_type, action, metadata, timestamp_utc)
VALUES ('system', 'PURGE', jsonb_build_object(
    'user_sessions_purged', (SELECT count(*) FROM user_sessions WHERE expires_at < now() - INTERVAL '1 day'),
    'api_logs_purged', (SELECT count(*) FROM api_logs WHERE created_at < now() - INTERVAL '90 days')
), now());
```

### 6.3 GDPR Data Subject Requests

When a data subject (customer) requests erasure:

```
Step 1: Receive and log the request
   Record: requester identity, date, specific data requested for deletion.

Step 2: Identify all data associated with the subject
   Search: customers, customer_addresses, sales_orders, invoices, payments,
           audit_logs, pos_transactions, user_sessions.

Step 3: Classify data by retention tier
   - Financial/tax records (Tier 1): CANNOT delete during 6-year retention
   - Business records (Tier 2): Can delete after relationship + retention
   - Operational data (Tier 3/4): Can delete immediately

Step 4: For data under legal retention hold
   Anonymize personal identifiers instead of deleting:

   UPDATE customers SET
       first_name = 'REDACTED',
       last_name = 'REDACTED',
       email = 'redacted_' || id || '@deleted.local',
       phone = NULL,
       tax_id = NULL,       -- Only if past retention period
       address = NULL,
       gdpr_erasure_date = now(),
       gdpr_erasure_request_id = '<request_id>'
   WHERE id = <customer_id>;

Step 5: For data past retention period
   DELETE completely and log the deletion in audit_logs.

Step 6: Respond to the requester
   Within 30 days, confirm what data was deleted and what data
   is retained under legal obligation (with expected deletion date).
```

---

## 7. Backup Retention Alignment

Backup retention must align with the data retention policy. Backups containing data subject to deletion requests require special handling.

| Backup Type | Retention | Contains Tier 1 Data | GDPR Consideration |
|-------------|-----------|---------------------|--------------------|
| Daily PostgreSQL | 7 days | Yes | Short retention minimizes exposure |
| Weekly PostgreSQL | 4 weeks | Yes | Short retention minimizes exposure |
| Monthly PostgreSQL | 12 months | Yes | May need to note GDPR requests for backup restores |
| RabbitMQ definitions | 30 days | No (config only) | No personal data |
| Docker volumes | 14 days | Yes | Short retention minimizes exposure |

**Important:** If a GDPR erasure request is fulfilled but the data still exists in backups, document the following:

1. The date of erasure from the live database
2. The backup files that may still contain the data
3. The date those backups will expire under the retention policy
4. If a backup is restored, the erasure must be re-applied

---

## 8. Implementation Checklist

### 8.1 Database Setup

- [ ] Create archive tables for all Tier 1 and Tier 2 entities
- [ ] Create `_all` views combining active and archive tables
- [ ] Add `archived_at` column to archive tables
- [ ] Set up table partitioning for `audit_logs` (by year)
- [ ] Create indexes on archive tables for date-range queries

### 8.2 Automated Jobs

- [ ] Daily purge job for Tier 3/4 data (3:00 AM)
- [ ] Monthly archival job for Tier 1/2 data (1st of month, 1:00 AM)
- [ ] Weekly audit log integrity check
- [ ] Quarterly retention compliance report

### 8.3 Monitoring

- [ ] Alert on archival job failures
- [ ] Alert on purge job failures
- [ ] Dashboard: active vs. archived record counts per entity
- [ ] Dashboard: storage growth trends

### 8.4 Documentation and Training

- [ ] Train operations team on GDPR erasure procedure
- [ ] Document the archive query patterns for compliance officers
- [ ] Include retention policy in new employee onboarding
- [ ] Annual review of retention periods against current regulations

---

## 9. Retention Period Quick Reference

```
Data Lifetime:

  Year 0     Year 1     Year 2     Year 3     Year 4     Year 5     Year 6     Year 7+
  |----------|----------|----------|----------|----------|----------|----------|---------->
  |<-- Active Table -->|<-- Archive Table --------------------------------->|  Purge
  |                    |                                                     |
  |  Hot data          |  Cold data, compliance-accessible                   |  Delete
  |  Fast queries      |  Index on date columns                              |  after
  |  Regular backups   |  Monthly backup coverage                            |  legal
  |                    |                                                     |  review

  Operational Data (Tier 3/4):
  |<- 30-90 days ->|  Purge automatically

  Customer PII (after relationship ends):
  |<-- 3 years -->|  Anonymize or delete (subject to Tier 1 holds)
```

---

## 10. Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | June 2026 | Platform Team | Initial data retention policy |

---

## 11. Related Documents

- [04 - RS.GE Technical Analysis](04-RSGE-TECHNICAL-ANALYSIS.md)
- [06 - Security Architecture](06-SECURITY-ARCHITECTURE.md)
- [11 - Disaster Recovery Plan](11-DISASTER-RECOVERY.md)
