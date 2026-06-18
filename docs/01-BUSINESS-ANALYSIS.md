# Business Analysis Document

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Draft — Awaiting Stakeholder Review

---

## 1. Executive Summary

This document presents the business analysis for an enterprise-grade Retail ERP platform designed for the Georgian market. The platform will serve as the single source of truth for all business operations of a national retail chain, including stores, warehouses, distribution, procurement, finance, CRM, and regulatory compliance.

Georgia's retail sector is experiencing rapid growth (9.8% YoY in 2025, projected GEL 24.2B FMCG turnover) with branded retail chains expanding from 40.4% to an estimated 52% market share by 2028. The top 4 players (Daily Group/Spar, Nikora, Ori Nabiji, Carrefour) control approximately 72-75% of organized retail. This growth creates demand for a modern, integrated ERP platform purpose-built for Georgian regulatory and business requirements.

The architecture is designed around Georgian Revenue Service (RS.GE) compliance as a core system component, not an add-on integration.

---

## 2. Market Analysis

### 2.1 Georgian Retail Landscape

| Metric | Value |
|--------|-------|
| FMCG Market Size (2025) | GEL 24.2 billion |
| YoY Growth (2025) | 9.8% |
| Projected Growth (2026) | 5.9% |
| Branded Chain Market Share (2025) | 40.4% |
| Branded Chain Market Share (2028, projected) | 52.0% |
| New Stores Opened (2024) | ~500 |
| E-Commerce Market (H1 2025) | GEL 2.1 billion |
| E-Commerce CAGR (2025-2030) | 20.5% |

### 2.2 Major Retail Chains

| Chain | Market Share | Stores | Notes |
|-------|-------------|--------|-------|
| Daily Group (Spar, Magniti, Ioli, etc.) | ~33% | 1,600+ | Largest holding, 18,000 employees |
| Ori Nabiji | ~20.5% | — | Second-largest modern retail |
| Nikora | ~18.7% | 600+ | Largest by store count |
| Carrefour Georgia | ~29% organized FMCG | — | Hypermarket segment |
| Goodwill | Niche | 8 | Uses ERP system |

### 2.3 Consumer Payment Landscape

- Georgia is ranked **#1 globally** in contactless payment penetration (Visa)
- 60%+ of online purchases are made via mobile devices
- Cash remains relevant outside urban centers
- Apple Pay / Google Pay growing in Tbilisi

### 2.4 Payment Gateway Market

| Provider | Market Share | Commission (Local) | Commission (International) |
|----------|-------------|-------------------|---------------------------|
| Bank of Georgia (iPay) | 60% | 2.0-2.5% | 2.8-3.2% |
| TBC Bank (TBC Pay) | 30% | 1.9-2.4% | — |
| Liberty Pay | Growing | — | — |

---

## 3. Regulatory Environment

### 3.1 Tax Framework

| Tax Type | Rate | Threshold | Filing |
|----------|------|-----------|--------|
| VAT | 18% (single rate) | GEL 100,000/12 months | Monthly, by 15th |
| Small Business (IE) | 1% gross revenue | GEL 30,001 – 500,000 | Monthly, by 15th |
| Micro Business | 0% | Under GEL 30,000 | Annual |
| Corporate Income Tax | 15% (on distribution) | All companies | Annual |

### 3.2 Key Compliance Requirements

1. **Invoice Upload Mandate**: All invoices must be uploaded to RS.GE within 30 calendar days
2. **Penalty for Non-Compliance**: 100% of VAT amount for failure to issue invoices within 30 days
3. **Electronic Waybills**: Required for all goods movement/transportation
4. **Qualified Electronic Signatures (QES)**: Required for documents submitted to RS.GE
5. **Record Retention**: Minimum 6 years from end of accounting period (we target 10 years)
6. **Payment Method Reporting**: Income must be broken down by payment method (cash, POS, bank transfer)
7. **TIN Validation**: 9-digit Taxpayer Identification Number required for all business partners
8. **VAT Payer Verification**: Must verify VAT status of business partners

### 3.3 Currency Requirements

- **ISO 4217**: GEL (Georgian Lari)
- **Subdivision**: 100 tetri = 1 lari
- **Symbol**: ₾
- **Precision**: 2 decimal places required
- **Denominations**: Coins (5, 10, 20, 50 tetri; 1, 2 lari), Banknotes (₾5-₾100)

### 3.4 Labor Law Requirements

| Requirement | Value |
|-------------|-------|
| Standard Work Week | 40 hours (5×8) |
| Flexible Operations | Up to 48 hours/week |
| Rest Between Days | Minimum 12 hours |
| Weekly Rest | Minimum 24 hours |
| Annual Leave | Minimum 24 calendar days |
| Payment Frequency | Monthly |
| Payment Currency | GEL required |
| Notice Period | 30 days |
| Minimum Wage (Private) | 20 GEL/month |

---

## 4. RS.GE Integration Analysis

### 4.1 Available RS.GE Services

RS.GE provides SOAP-based web services. Two primary service endpoints have been identified:

#### WayBill Service
- **Endpoint**: `https://services.rs.ge/WayBillService/WayBillService.asmx`
- **Protocol**: SOAP/XML
- **Operations**: 60 operations identified
- **Namespace**: `http://tempuri.org/`

#### SpecInvoices Service
- **Endpoint**: `https://webserv.rs.ge/specinvoices/SpecInvoicesService.asmx`
- **Protocol**: SOAP/XML
- **Operations**: Invoice management operations

### 4.2 RS.GE API Operations Catalog

#### Authentication & User Management
| Operation | Purpose |
|-----------|---------|
| `create_service_user` | Register service user for API access |
| `update_service_user` | Update service user credentials |
| `chek_service_user` | Validate authentication pair |
| `get_service_users` | List registered service users |
| `what_is_my_ip` | Detect caller's IP (required for registration) |

#### Waybill Lifecycle Operations
| Operation | Purpose |
|-----------|---------|
| `save_waybill` | Create or update a waybill |
| `send_waybill` | Submit waybill for processing |
| `confirm_waybill` | Buyer confirms receipt of goods |
| `close_waybill` | Close completed waybill |
| `close_waybill_transporter` | Transporter closes waybill |
| `close_waybill_vd` | Close waybill (VD variant) |
| `reject_waybill` | Reject a waybill |
| `ref_waybill` | Reference/link waybill |
| `del_waybill` | Delete draft waybill |

#### Waybill Query Operations
| Operation | Purpose |
|-----------|---------|
| `get_waybill` | Get single waybill by ID |
| `get_waybill_by_number` | Get waybill by number |
| `get_waybills` / `get_waybills_ex` / `get_waybills_v1` | List waybills with filters |
| `get_buyer_waybills` / `get_buyer_waybills_ex` | Get waybills as buyer |
| `get_transporter_waybills` | Get waybills as transporter |
| `get_waybill_goods_list` | Get goods on a waybill |
| `get_buyer_waybilll_goods_list` | Get goods list (buyer view) |
| `get_adjusted_waybill` / `get_adjusted_waybills` | Get adjustment waybills |
| `get_c_waybill` | Get consolidated waybill |

#### Waybill Templates
| Operation | Purpose |
|-----------|---------|
| `save_waybill_tamplate` | Save reusable waybill template |
| `get_waybill_tamplate` / `get_waybill_tamplates` | Retrieve templates |
| `delete_waybill_tamplate` | Delete template |

#### Transporter Operations
| Operation | Purpose |
|-----------|---------|
| `save_waybill_transporter` | Assign transporter to waybill |
| `send_waybill_transporter` | Send waybill to transporter |
| `close_waybill_transporter` | Transporter closes delivery |

#### Reference Data
| Operation | Purpose |
|-----------|---------|
| `get_waybill_types` | Waybill type classifications |
| `get_waybill_units` | Units of measurement |
| `get_trans_types` | Transportation types |
| `get_wood_types` | Wood product classifications |
| `get_akciz_codes` | Excise tax codes |
| `get_error_codes` | Error code dictionary |

#### Validation & Lookup
| Operation | Purpose |
|-----------|---------|
| `get_name_from_tin` | Resolve name from TIN |
| `get_tin_from_un_id` | Get TIN from unique ID |
| `get_payer_type_from_un_id` | Get payer type |
| `is_vat_payer` | Check VAT payer status |
| `is_vat_payer_tin` | Check VAT status by TIN |

#### Barcode & Vehicle Management
| Operation | Purpose |
|-----------|---------|
| `save_bar_code` / `get_bar_codes` / `delete_bar_code` | Product barcode management |
| `save_car_numbers` / `get_car_numbers` / `delete_car_numbers` | Vehicle plate management |

#### Output & Utility
| Operation | Purpose |
|-----------|---------|
| `get_print_pdf` | Generate printable PDF |
| `get_server_time` | Server time synchronization |
| `save_invoice` | Save invoice document |

### 4.3 Authentication Model

1. Caller retrieves public IP via `what_is_my_ip`
2. Service user created via `create_service_user` with: username, password, IP whitelist, description
3. All subsequent API calls require `su` (service user) and `sp` (service password) parameters
4. IP whitelisting enforced — requests from non-registered IPs are rejected

### 4.4 Waybill Lifecycle

```
Draft → Saved → Sent → Confirmed → Closed
                  ↓         ↓
               Rejected   Adjusted
```

1. **Draft**: Waybill created locally in ERP
2. **Saved**: `save_waybill` — registered with RS.GE, receives waybill ID
3. **Sent**: `send_waybill` — dispatched with goods
4. **Confirmed**: `confirm_waybill` — buyer acknowledges receipt
5. **Closed**: `close_waybill` — transaction completed
6. **Rejected**: `reject_waybill` — buyer refuses goods
7. **Adjusted**: Corrections via adjustment waybills

### 4.5 Compliance Risks

| Risk | Severity | Mitigation |
|------|----------|------------|
| Invoice not uploaded within 30 days | Critical | Automated queue with deadline tracking |
| RS.GE service unavailable | High | Queue-based architecture with automatic retry |
| IP address change breaks API access | High | Monitoring + automated re-registration |
| Invalid TIN on invoices | Medium | Real-time validation via `get_name_from_tin` |
| VAT status changes for partners | Medium | Periodic re-validation via `is_vat_payer` |
| QES certificate expiration | Medium | Certificate lifecycle management |
| Waybill not created for goods movement | Critical | Enforce waybill creation in all dispatch workflows |
| Incorrect excise codes | High | Reference data sync via `get_akciz_codes` |
| Audit trail gaps | Critical | Immutable event log for all RS.GE communications |

---

## 5. Stakeholder Analysis

### 5.1 Primary Stakeholders

| Stakeholder | Role | Key Concerns |
|-------------|------|--------------|
| Store Managers | Daily operations | POS reliability, inventory accuracy, reporting |
| Cashiers | Transaction processing | Speed, simplicity, error recovery |
| Warehouse Staff | Goods management | Receiving, dispatch, counting, accuracy |
| Procurement Team | Purchasing | Supplier management, cost optimization |
| Accountants | Financial management | Accuracy, compliance, audit readiness |
| Tax Compliance Officer | RS.GE compliance | Invoice uploads, VAT reporting, waybills |
| Executive Management | Strategic oversight | KPIs, profitability, growth |
| IT Department | System maintenance | Reliability, security, scalability |

### 5.2 External Stakeholders

| Stakeholder | Integration | Concerns |
|-------------|------------|----------|
| Georgian Revenue Service (RS.GE) | SOAP API | Compliance, timely reporting |
| Banks (BoG, TBC, Liberty) | Payment APIs | Transaction processing, reconciliation |
| Suppliers | Portal / EDI | Purchase orders, invoices, payments |
| Customers | CRM / Loyalty | Service quality, loyalty rewards |
| Auditors | Read-only access | Audit trails, financial accuracy |
| Payment Terminal Providers | Hardware integration | Transaction processing |

---

## 6. Business Process Analysis

### 6.1 Core Business Processes

#### P1: Procure-to-Pay
```
Purchase Requisition → Approval → Purchase Order → Supplier Confirmation
→ Goods Receipt (+ RS.GE Waybill) → Quality Check → Stock Update
→ Supplier Invoice Match → Payment Scheduling → Payment Execution
→ Accounting Entry → VAT Recovery
```

#### P2: Order-to-Cash (POS)
```
Product Scan → Price Lookup → Discount/Promotion Applied
→ Payment (Cash/Card/Mixed) → Fiscal Receipt Generation
→ RS.GE Invoice Upload → Inventory Deduction → Accounting Entry
→ Daily Cash Closing → Bank Reconciliation
```

#### P3: Inventory Movement
```
Transfer Request → Approval → RS.GE Waybill Creation
→ Goods Dispatch (Source) → Transportation → Goods Receipt (Destination)
→ RS.GE Waybill Confirmation → Stock Adjustment → Accounting Entry
```

#### P4: Financial Reporting
```
Daily Transactions → General Ledger Entries → Trial Balance
→ Adjustments → Financial Statements (P&L, Balance Sheet, Cash Flow)
→ VAT Declaration → RS.GE Submission → Audit Trail
```

### 6.2 Transaction Traceability Chain

Every transaction must maintain a complete, auditable chain:

```
Purchase Order
  → Goods Receipt Note
    → RS.GE Waybill (confirmed)
      → Inventory Movement Record
        → Sale Transaction
          → Fiscal Receipt
            → RS.GE Invoice Upload
              → Accounting Journal Entry
                → VAT Declaration Line Item
                  → Tax Report Submission
```

---

## 7. Functional Requirements Summary

### 7.1 Module Priority Matrix

| Module | Business Priority | Compliance Impact | MVP |
|--------|------------------|-------------------|-----|
| POS | Critical | High (fiscal receipts) | Yes |
| Inventory Management | Critical | High (waybills) | Yes |
| RS.GE Compliance Layer | Critical | Critical | Yes |
| Product Management | Critical | Medium | Yes |
| Pricing Management | High | Low | Yes |
| Warehouse Management | High | High (waybills) | Yes |
| Procurement | High | Medium | Yes |
| Accounting & Finance | Critical | Critical (VAT) | Yes |
| CRM | Medium | Low | Phase 2 |
| Supplier Management | Medium | Medium | Phase 2 |
| Reporting & BI | High | Medium | Phase 2 |
| Approval Workflows | Medium | Low | Phase 2 |
| Notifications | Medium | Low | Phase 2 |
| AI Features | Low | None | Phase 3 |
| Mobile App | Medium | Low | Phase 3 |

### 7.2 Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| Stores Supported | 100+ |
| Warehouses Supported | 20+ |
| Concurrent Users | 500+ |
| Product Catalog | 1,000,000+ SKUs |
| Annual Transactions | 10,000,000+ |
| System Availability | 99.9% |
| POS Transaction Time | < 2 seconds |
| Report Generation | < 30 seconds |
| Data Retention | 10 years |
| RS.GE Sync Latency | < 5 minutes (normal), queued when unavailable |
| Offline POS Operation | 72 hours minimum |
| Recovery Time Objective | < 1 hour |
| Recovery Point Objective | < 5 minutes |

---

## 8. Risk Assessment

### 8.1 Business Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| RS.GE API changes without notice | Medium | High | Abstraction layer, version monitoring |
| Regulatory changes to tax code | Medium | High | Configuration-driven compliance rules |
| Internet connectivity issues at stores | High | High | Offline-first POS architecture |
| Data loss during synchronization | Low | Critical | Event sourcing, conflict resolution |
| Security breach / data leak | Low | Critical | Encryption, RBAC, audit logging |
| Vendor lock-in | Medium | Medium | Open standards, modular architecture |
| Performance degradation at scale | Medium | High | Load testing, horizontal scaling |
| Key personnel dependency | Medium | Medium | Documentation, knowledge sharing |

### 8.2 Technical Risks

| Risk | Probability | Impact | Mitigation |
|------|------------|--------|------------|
| SOAP API complexity | High | Medium | RS.GE abstraction service with retry logic |
| IP whitelisting breaks on cloud | Medium | High | Static IP allocation, monitoring |
| QES integration complexity | Medium | Medium | Partner with certified QTSP provider |
| Offline/online sync conflicts | High | High | CRDT-based conflict resolution, last-write-wins with audit |
| Multi-tenant data isolation | Medium | Critical | Schema-per-tenant with shared infrastructure |
| Legacy system migration | High | High | Phased migration with parallel running |

---

## 9. Assumptions and Constraints

### 9.1 Assumptions

1. RS.GE SOAP APIs will remain available and backwards-compatible
2. Internet connectivity is available at warehouses and HQ (stores may be intermittent)
3. Staff have basic computer literacy
4. Georgian language (ქართული) is the primary UI language, with English as secondary
5. GEL is the primary operating currency
6. The business operates primarily within Georgia
7. PostgreSQL can handle the required transaction volumes
8. Cloud infrastructure (AWS/Azure) is acceptable for hosting

### 9.2 Constraints

1. All fiscal documents must comply with Georgian Tax Code
2. RS.GE integration is SOAP-based (not REST)
3. QES requires integration with a Georgian QTSP
4. Data must be stored within Georgia or a jurisdiction with adequate data protection
5. System must support Georgian Unicode characters (UTF-8)
6. Invoice upload deadline of 30 days is non-negotiable
7. Waybills must be created before goods movement begins

---

## 10. Recommendations

### 10.1 Architecture Recommendations

1. **Modular Monolith over Microservices**: For a team building an ERP, a modular monolith provides better transactional consistency, simpler deployment, and easier debugging while maintaining clean module boundaries. Microservices can be extracted later as specific modules need independent scaling.

2. **RS.GE First Design**: The compliance layer must be a core module, not an integration. Every inventory movement, sale, and purchase must route through compliance validation.

3. **Event-Driven Architecture**: Use an internal event bus to decouple modules while maintaining transaction consistency within the monolith. This enables reliable RS.GE queue processing and audit trail generation.

4. **Offline-First POS**: POS terminals must operate independently with local storage, syncing when connectivity is available. This is non-negotiable for Georgian retail where connectivity can be unreliable.

### 10.2 Technology Recommendations

1. **Backend**: .NET 9 (ASP.NET Core) — strong SOAP support (critical for RS.GE), excellent performance, mature ecosystem for enterprise applications, built-in dependency injection
2. **Database**: PostgreSQL 17 — proven at scale, excellent JSON support, partitioning for large tables, strong community
3. **Frontend**: React 19 + TypeScript — large talent pool, component ecosystem, strong typing
4. **Mobile**: Flutter — single codebase for Android (priority) and iOS, offline capabilities
5. **Message Queue**: RabbitMQ — reliable message delivery for RS.GE communication queue
6. **Cache**: Redis — session management, reference data caching, rate limiting
7. **Search**: Elasticsearch — product search across 1M+ SKUs
8. **Infrastructure**: Docker + Kubernetes on AWS/Azure

### 10.3 Phased Implementation

- **Phase 1 (MVP, 6-8 months)**: Core POS, Inventory, Warehouse, Procurement, RS.GE Compliance, Basic Accounting, Product & Pricing Management
- **Phase 2 (4-6 months)**: CRM, Supplier Portal, Advanced Reporting, Approval Workflows, Notifications
- **Phase 3 (4-6 months)**: Mobile App, AI Features, BI Platform, E-Commerce Integration
- **Phase 4 (Ongoing)**: Multi-Company, Franchise Management, Advanced Analytics, Marketplace Integrations

---

## Appendix A: Georgian Tax Calendar

| Deadline | Obligation | Frequency |
|----------|-----------|-----------|
| 15th of each month | VAT declaration (local businesses) | Monthly |
| 15th of each month | 1% tax payment (small business) | Monthly |
| 20th after quarter | VAT filing (digital services) | Quarterly |
| End of month after quarter | VAT payment (digital services) | Quarterly |
| October 1st | Annual financial statements | Annual |
| Within 30 days | Invoice upload to RS.GE | Per transaction |
| Before dispatch | Waybill creation on RS.GE | Per goods movement |

## Appendix B: Glossary

| Term | Georgian | Description |
|------|---------|-------------|
| Waybill | სასაქონლო ზედნადები | Goods movement document required by RS.GE |
| TIN | საიდენტიფიკაციო ნომერი | 9-digit Taxpayer Identification Number |
| GEL | ლარი | Georgian Lari (national currency) |
| Tetri | თეთრი | 1/100 of a Lari |
| RS.GE | შემოსავლების სამსახური | Georgian Revenue Service |
| QES | კვალიფიციური ელექტრონული ხელმოწერა | Qualified Electronic Signature |
| IE | ინდივიდუალური მეწარმე | Individual Entrepreneur |
| VAT | დღგ (დამატებული ღირებულების გადასახადი) | Value Added Tax (18%) |
