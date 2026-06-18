# RS.GE Technical Analysis & Compliance Risk Assessment

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Draft — Awaiting Review

---

## 1. RS.GE Service Overview

The Georgian Revenue Service (RS.GE / შემოსავლების სამსახური) provides electronic services through SOAP-based web services. These services are the mandatory integration point for all fiscally relevant business operations in Georgia.

### 1.1 Available Service Endpoints

| Service | Endpoint | Purpose |
|---------|----------|---------|
| WayBill Service | `https://services.rs.ge/WayBillService/WayBillService.asmx` | Electronic waybills, goods movement, invoices, reference data |
| SpecInvoices Service | `https://webserv.rs.ge/specinvoices/SpecInvoicesService.asmx` | Special invoice operations |
| eServices Portal | `https://eservices.rs.ge/app/` | Manual portal access, documentation downloads |
| VAT Portal | `https://nr.rs.ge/` | Digital services VAT reporting |

### 1.2 Protocol & Technology

| Aspect | Detail |
|--------|--------|
| Protocol | SOAP 1.1 / 1.2 |
| Transport | HTTPS |
| Data Format | XML |
| WSDL | Available at each `.asmx?WSDL` endpoint |
| Namespace | `http://tempuri.org/` |
| Authentication | Service User credentials (su/sp parameters) |
| IP Security | IP whitelisting per service user |

---

## 2. Authentication & Registration Process

### 2.1 Service User Setup

```
Step 1: Obtain public IP
    → Call: what_is_my_ip
    → Response: caller's external IP address

Step 2: Create service user (one-time, via RS.GE portal or API)
    → Call: create_service_user
    → Parameters:
        - user_name: RS.GE portal username
        - user_password: RS.GE portal password
        - ip: server's public IP (from Step 1)
        - name: descriptive name for this integration
        - su: new service username
        - sp: new service password
    → Response: { payer_id, user_id }

Step 3: Store credentials securely
    → su (service username) and sp (service password)
    → Used in ALL subsequent API calls

Step 4: Verify credentials
    → Call: chek_service_user
    → Confirms authentication pair is valid
```

### 2.2 IP Whitelisting Implications

**Critical for cloud deployment**:
- RS.GE validates the caller's IP against the registered IP
- Cloud environments with dynamic IPs will break authentication
- **Mitigation**: Allocate static Elastic IP (AWS) or Reserved IP (Azure) for the RS.GE worker pod
- **Monitoring**: Alert on IP changes; auto-update via `update_service_user` if IP changes

### 2.3 Multiple Service Users

- Use `get_service_users` to list all registered service users
- Consider separate service users for different operations (waybills vs. invoices) for better audit separation
- Each service user can have different IP whitelisting

---

## 3. WayBill Service — Complete API Analysis

### 3.1 Waybill Types (from `get_waybill_types`)

Based on Georgian legislation, waybill types include:
1. **Internal** — goods movement within same legal entity (warehouse-to-store, store-to-store)
2. **Outgoing** — goods sold/dispatched to another legal entity
3. **Incoming** — goods received from another legal entity
4. **Return** — return of goods to supplier
5. **Distribution** — distribution/delivery operations
6. **Without transportation** — for certain exempt movements

### 3.2 Waybill Lifecycle

```
              save_waybill
    DRAFT ────────────────→ SAVED (has RS.GE ID)
                              │
                    send_waybill / send_waybill_transporter
                              │
                              ▼
                           ACTIVE
                              │
                   ┌──────────┼──────────┐
                   │          │          │
          confirm_waybill  reject_waybill  (timeout)
                   │          │          │
                   ▼          ▼          ▼
              CONFIRMED   REJECTED   EXPIRED
                   │
             close_waybill
                   │
                   ▼
                CLOSED
```

### 3.3 Waybill Data Structure

```xml
<!-- Typical save_waybill request structure -->
<save_waybill>
    <su>service_username</su>
    <sp>service_password</sp>
    <waybill_type>1</waybill_type>
    <buyer_tin>123456789</buyer_tin>
    <seller_un_id></seller_un_id>
    <start_address>Tbilisi, Rustaveli Ave 12</start_address>
    <end_address>Kutaisi, Tsereteli St 5</end_address>
    <transport_type_id>1</transport_type_id>
    <car_number>AA-123-BB</car_number>
    <driver_tin>987654321</driver_tin>
    <comment>Transfer order #TRN-2026-0042</comment>
    <goods>
        <goods_item>
            <product_name>Product Name</product_name>
            <unit_id>99</unit_id>
            <quantity>100</quantity>
            <price>15.50</price>
            <bar_code>5901234123457</bar_code>
        </goods_item>
    </goods>
</save_waybill>
```

### 3.4 Complete Operations Reference

#### Waybill CRUD

| Operation | Input | Output | Notes |
|-----------|-------|--------|-------|
| `save_waybill` | Waybill data + goods list | Waybill ID | Creates or updates draft |
| `get_waybill` | Waybill ID | Full waybill data | Single waybill lookup |
| `get_waybill_by_number` | Waybill number | Full waybill data | Lookup by number |
| `get_waybills` | Date range, filters | Waybill list | Paginated listing |
| `get_waybills_ex` | Extended filters | Waybill list | Extended query |
| `get_waybills_v1` | Date range, filters | Waybill list | V1 query format |
| `del_waybill` | Waybill ID | Success/failure | Only draft waybills |

#### Waybill State Transitions

| Operation | From State | To State | Notes |
|-----------|-----------|----------|-------|
| `send_waybill` | SAVED | ACTIVE | Dispatches goods |
| `send_waybil_vd` | SAVED | ACTIVE | VD variant |
| `confirm_waybill` | ACTIVE | CONFIRMED | Buyer confirms receipt |
| `close_waybill` | CONFIRMED | CLOSED | Finalizes |
| `close_waybill_transporter` | ACTIVE | CLOSED | Transporter closes |
| `close_waybill_vd` | ACTIVE | CLOSED | VD variant |
| `reject_waybill` | ACTIVE | REJECTED | Buyer rejects |
| `ref_waybill` | Any | REFERENCED | Links to another waybill |
| `ref_waybill_vd` | Any | REFERENCED | VD variant |

#### Counterparty Queries

| Operation | Purpose |
|-----------|---------|
| `get_buyer_waybills` | Get waybills where current entity is buyer |
| `get_buyer_waybills_ex` | Extended buyer waybill query |
| `get_transporter_waybills` | Get waybills where current entity is transporter |
| `get_buyer_waybilll_goods_list` | Get goods list from buyer perspective |
| `get_waybill_goods_list` | Get goods on any waybill |

#### Adjustment & Consolidated

| Operation | Purpose |
|-----------|---------|
| `get_adjusted_waybill` | Get single adjustment waybill |
| `get_adjusted_waybills` | List adjustment waybills |
| `get_c_waybill` | Get consolidated waybill |

#### Templates (Reusable Waybill Templates)

| Operation | Purpose |
|-----------|---------|
| `save_waybill_tamplate` | Save template for recurring shipments |
| `get_waybill_tamplate` | Get single template |
| `get_waybill_tamplates` | List all templates |
| `delete_waybill_tamplate` | Remove template |

#### Validation & Lookup Services

| Operation | Input | Output | Usage in ERP |
|-----------|-------|--------|-------------|
| `get_name_from_tin` | TIN (9 digits) | Entity name | Supplier/customer registration validation |
| `get_tin_from_un_id` | Unique ID | TIN | Cross-reference lookup |
| `get_payer_type_from_un_id` | Unique ID | Payer type | Entity classification |
| `is_vat_payer` | TIN | Boolean + date | Verify VAT status before invoicing |
| `is_vat_payer_tin` | TIN | Boolean | Quick VAT check |

#### Reference Data

| Operation | Output | Caching Strategy |
|-----------|--------|-----------------|
| `get_waybill_types` | Type ID → Name mapping | Cache for 24 hours |
| `get_waybill_units` | Unit ID → Name mapping | Cache for 24 hours |
| `get_trans_types` | Transport type ID → Name | Cache for 24 hours |
| `get_wood_types` | Wood type classifications | Cache for 24 hours |
| `get_akciz_codes` | Excise tax codes | Cache for 24 hours |
| `get_error_codes` | Error code → Description | Cache for 24 hours |

#### Barcode & Vehicle Management

| Operation | Purpose |
|-----------|---------|
| `save_bar_code` | Register product barcode with RS.GE |
| `get_bar_codes` | List registered barcodes |
| `delete_bar_code` | Remove barcode registration |
| `save_car_numbers` | Register vehicle for waybills |
| `get_car_numbers` | List registered vehicles |
| `delete_car_numbers` | Remove vehicle registration |

#### Utility

| Operation | Purpose |
|-----------|---------|
| `get_print_pdf` | Generate printable waybill PDF |
| `get_server_time` | Time synchronization |
| `what_is_my_ip` | IP detection for registration |
| `save_invoice` | Submit invoice to RS.GE |
| `get_waybills_medicaments_moh` | Ministry of Health medicament waybills |

---

## 4. Invoice Service Analysis

### 4.1 SpecInvoices Service

| Endpoint | `https://webserv.rs.ge/specinvoices/SpecInvoicesService.asmx` |
|----------|--------------------------------------------------------------|
| Purpose | Tax invoice management, special invoice operations |
| Protocol | SOAP/XML |

### 4.2 Invoice Requirements

Every sale to a VAT-registered entity requires a tax invoice uploaded to RS.GE within 30 days.

**Required Invoice Fields**:
- Seller TIN and name
- Buyer TIN and name
- Invoice number and date
- Line items: description, quantity, unit, price
- VAT amount per line
- Total amounts (subtotal, VAT, gross)
- Payment method

**Key Rules**:
1. Invoices must be uploaded within 30 calendar days of the transaction
2. Failure to upload results in a penalty of 100% of the VAT amount
3. Both parties (seller and buyer) can view the invoice on RS.GE
4. Invoices can be corrected via adjustment invoices
5. Credit notes must reference the original invoice

---

## 5. VAT Compliance Architecture

### 5.1 VAT Calculation Rules

```
Georgia VAT Rules:
├── Standard Rate: 18% on all taxable supplies
├── No reduced rates exist
├── Exempt categories:
│   ├── Medical services (licensed institutions)
│   ├── Educational services (accredited providers)
│   ├── Financial/banking services
│   ├── Land transactions
│   └── Exports (zero-rated, not exempt)
│
├── Registration:
│   ├── Mandatory: turnover > GEL 100,000 in any 12-month period
│   ├── Multiple entities: combined turnover counts
│   ├── Voluntary registration: allowed
│   └── Import/excise production: immediate registration
│
├── Filing:
│   ├── Local businesses: monthly, by 15th
│   ├── Digital services: quarterly, by 20th after quarter
│   └── Payment: by end of month following period
│
└── Input VAT:
    ├── Recoverable for registered VAT payers
    ├── Excess input → refund or offset
    └── Reverse charge for imported services
```

### 5.2 ERP VAT Processing Flow

```
Transaction Created
    │
    ▼
┌─────────────────────────────┐
│ VAT Determination Engine    │
│                             │
│ 1. Is seller VAT-registered?│──→ No: skip VAT, apply alternative regime
│ 2. Is item exempt?          │──→ Yes: zero VAT, record exemption
│ 3. Is reverse charge?       │──→ Yes: buyer self-assesses
│ 4. Calculate 18% VAT        │
│ 5. Generate invoice data    │
│ 6. Calculate deadline       │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Fiscal Document Generator   │
│                             │
│ - Create fiscal_documents   │
│ - Set submission_deadline   │
│ - Enqueue for RS.GE upload  │
└─────────────┬───────────────┘
              │
              ▼
┌─────────────────────────────┐
│ Monthly VAT Aggregation     │
│                             │
│ - Sum output VAT (sales)    │
│ - Sum input VAT (purchases) │
│ - Calculate net position    │
│ - Generate VAT declaration  │
│ - Submit by 15th            │
└─────────────────────────────┘
```

---

## 6. When Waybills Are Required

### 6.1 Business Scenarios Requiring Waybills

| Scenario | Waybill Type | Required? | Notes |
|----------|-------------|-----------|-------|
| Supplier delivers to warehouse | Incoming | Yes | Supplier creates, buyer confirms |
| Warehouse to store transfer | Internal | Yes | Same legal entity |
| Store to store transfer | Internal | Yes | Same legal entity |
| Return goods to supplier | Return | Yes | Buyer initiates |
| Sale with delivery to customer | Outgoing | Yes | If goods are transported |
| Over-the-counter retail sale | N/A | No | POS receipt sufficient |
| Write-off / disposal | N/A | Depends | May require documentation |

### 6.2 ERP Integration Points

| ERP Operation | Waybill Action | Timing |
|---------------|---------------|--------|
| Purchase Order → Goods Receipt | `confirm_waybill` (buyer) | On receiving goods |
| Transfer Order created | `save_waybill` (internal) | Before dispatch |
| Transfer Order dispatched | `send_waybill` | At dispatch |
| Transfer Order received | `confirm_waybill` + `close_waybill` | On receipt |
| Supplier Return initiated | `save_waybill` (return type) | Before dispatch |
| Customer delivery | `save_waybill` (outgoing) | Before dispatch |

---

## 7. Compliance Risk Matrix

### 7.1 Critical Risks (Immediate Financial/Legal Impact)

| Risk ID | Risk | Likelihood | Impact | Controls |
|---------|------|-----------|--------|----------|
| CR-01 | Invoice not uploaded to RS.GE within 30 days | Medium | Critical (100% VAT penalty) | Deadline tracker, escalation alerts at 20/25/28 days |
| CR-02 | Goods moved without waybill | Medium | Critical (fine + seizure risk) | Block dispatch without waybill confirmation |
| CR-03 | Incorrect VAT calculation | Low | Critical (audit exposure) | Automated VAT engine, no manual override without approval |
| CR-04 | Missing audit trail | Low | Critical (audit failure) | Immutable event log, no delete operations on fiscal data |
| CR-05 | RS.GE service prolonged outage | Low | Critical (operations halt) | Queue-based architecture, 72hr offline buffer |

### 7.2 High Risks (Operational/Regulatory Impact)

| Risk ID | Risk | Likelihood | Impact | Controls |
|---------|------|-----------|--------|----------|
| HR-01 | RS.GE API breaking change | Medium | High | Abstraction layer, version detection, monitoring |
| HR-02 | IP whitelist disrupted | Medium | High | Static IP, monitoring, automated re-registration |
| HR-03 | QES certificate expiry | Medium | High | Certificate lifecycle management, 30-day renewal alerts |
| HR-04 | Duplicate waybill submission | Medium | High | Idempotency keys, deduplication check |
| HR-05 | TIN validation failure at transaction time | High | Medium | Cache TIN lookups, graceful fallback |
| HR-06 | VAT status change mid-period | Medium | High | Periodic re-validation (daily batch) |

### 7.3 Medium Risks (Operational Efficiency)

| Risk ID | Risk | Likelihood | Impact | Controls |
|---------|------|-----------|--------|----------|
| MR-01 | RS.GE response time degradation | High | Medium | Timeout handling, async processing |
| MR-02 | Reference data staleness | Medium | Medium | Daily sync of units, transport types, error codes |
| MR-03 | Error code interpretation | Medium | Medium | Error code lookup table, automated categorization |
| MR-04 | Barcode mismatch with RS.GE registry | Medium | Low | Sync barcodes via save_bar_code on product creation |
| MR-05 | Timezone discrepancy | Low | Medium | UTC storage, RS.GE server time sync |

---

## 8. RS.GE Integration Implementation Plan

### 8.1 Phase 1 — Foundation (Weeks 1-4)

1. Set up SOAP client infrastructure (.NET WCF/HttpClient for SOAP)
2. Implement service user authentication
3. Implement reference data synchronization (units, types, error codes)
4. Implement TIN lookup and VAT payer verification
5. Build communication logging infrastructure
6. Build retry queue with RabbitMQ

### 8.2 Phase 2 — Waybill Integration (Weeks 5-8)

1. Implement waybill creation (save_waybill)
2. Implement waybill state transitions (send, confirm, close, reject)
3. Integrate with transfer orders
4. Integrate with goods receipt (purchase receiving)
5. Implement waybill PDF generation
6. Build waybill monitoring dashboard

### 8.3 Phase 3 — Invoice Integration (Weeks 9-12)

1. Implement invoice submission (save_invoice)
2. Implement 30-day deadline tracking
3. Integrate with POS transactions
4. Integrate with accounts receivable
5. Build invoice compliance dashboard
6. Implement deadline escalation alerts

### 8.4 Phase 4 — VAT & Reporting (Weeks 13-16)

1. Implement VAT calculation engine
2. Build monthly VAT declaration generator
3. Implement input/output VAT reconciliation
4. Build compliance reporting suite
5. Integration testing with RS.GE staging environment

---

## 9. Qualified Electronic Signature (QES) Integration

### 9.1 Legal Framework

Georgia's "Law on Electronic Signatures and Electronic Documents" (harmonized with EU eIDAS Regulation 910/2014) requires QES for documents submitted to administrative bodies, including RS.GE.

### 9.2 Technical Requirements

| Requirement | Detail |
|-------------|--------|
| Signature Standard | Qualified Electronic Signature (QES) |
| Activation | Georgian ID card or residence card |
| PIN Provision | Public Service Halls or SDA |
| QTSP | Must be certified by Data Exchange Agency (Ministry of Justice) |
| Properties | Unique identification, non-repudiation, tamper detection |

### 9.3 Integration Approach

1. Partner with a certified Georgian QTSP
2. Implement signing module using QTSP's SDK/API
3. Apply QES to tax declarations and financial submissions
4. Store signed documents with verification metadata
5. Implement certificate lifecycle management (renewal, revocation monitoring)

---

## 10. Recommendations for Architecture

### 10.1 RS.GE Abstraction Layer

Create a dedicated `RsGeService` that encapsulates all RS.GE communication:

```
IRsGeService
├── Waybills
│   ├── CreateWaybillAsync(WaybillDto)
│   ├── SendWaybillAsync(waybillId)
│   ├── ConfirmWaybillAsync(waybillId)
│   ├── CloseWaybillAsync(waybillId)
│   ├── RejectWaybillAsync(waybillId)
│   ├── GetWaybillAsync(waybillId)
│   └── GetWaybillsAsync(filters)
│
├── Invoices
│   ├── SubmitInvoiceAsync(InvoiceDto)
│   └── GetInvoicesAsync(filters)
│
├── Lookup
│   ├── GetNameFromTinAsync(tin)
│   ├── IsVatPayerAsync(tin)
│   └── GetTinFromUniqueIdAsync(uniqueId)
│
├── Reference
│   ├── SyncUnitsAsync()
│   ├── SyncTransportTypesAsync()
│   ├── SyncWaybillTypesAsync()
│   └── SyncErrorCodesAsync()
│
└── System
    ├── GetServerTimeAsync()
    ├── GetMyIpAsync()
    └── HealthCheckAsync()
```

### 10.2 Queue Architecture for RS.GE

```
Exchanges:
├── rsge.waybill (direct exchange)
│   ├── Queue: rsge.waybill.create
│   ├── Queue: rsge.waybill.send
│   ├── Queue: rsge.waybill.confirm
│   └── Queue: rsge.waybill.close
│
├── rsge.invoice (direct exchange)
│   └── Queue: rsge.invoice.submit
│
├── rsge.retry (delayed exchange)
│   └── Queue: rsge.retry.{delay} (1s, 2s, 4s, 8s, 16s, 32s, 60s, 300s)
│
└── rsge.dlq (dead letter exchange)
    └── Queue: rsge.dead-letter (manual resolution)
```

### 10.3 Monitoring & Alerting

Dedicated RS.GE monitoring dashboard showing:
- Queue depths (pending, retry, dead letter)
- Success/failure rates per operation type
- Average response times
- Invoice deadline countdown (items approaching 30-day limit)
- Waybill state distribution
- Last successful communication timestamp
- IP whitelist status
