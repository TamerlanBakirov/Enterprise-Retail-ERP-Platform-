# MVP Definition & Development Roadmap

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Living roadmap - implementation is in progress. Checkboxes below describe target scope and are not a release-completion claim; use the README production-readiness section and automated tests for current status.

---

## 1. MVP Scope Definition

### 1.1 MVP Objective

Deliver a functional retail ERP that can operate a single company with multiple stores and warehouses, with full RS.GE compliance, within 6-8 months.

### 1.2 MVP Includes

| Module | Scope | Priority |
|--------|-------|----------|
| **RS.GE Compliance Layer** | Full waybill lifecycle, invoice upload, VAT engine, audit log, retry queue | P0 |
| **Product Management** | Products, categories, variants, barcodes, basic images | P0 |
| **Pricing** | Price lists, store-specific pricing, basic promotions | P0 |
| **POS** | Sales, returns, mixed payments, fiscal receipt, daily closing | P0 |
| **Inventory** | Stock levels, receiving, dispatch, transfers, adjustments, stock count | P0 |
| **Warehouse** | Locations, receiving orders, shipping orders, transfer orders | P0 |
| **Procurement** | Purchase orders, goods receipt, supplier management (basic) | P0 |
| **Accounting** | Chart of accounts, journal entries (auto-generated), AR/AP, bank accounts | P1 |
| **Security** | JWT auth, RBAC, audit logging, 2FA | P0 |
| **Admin** | User management, store/warehouse setup, system configuration | P0 |
| **Basic Reporting** | Sales summary, inventory status, VAT summary, daily closing report | P1 |

### 1.3 MVP Excludes (Deferred to Later Phases)

| Feature | Phase |
|---------|-------|
| CRM & Loyalty Program | Phase 2 |
| Advanced Reporting & BI Dashboards | Phase 2 |
| Approval Workflows (configurable) | Phase 2 |
| Notification System (SMS/Email) | Phase 2 |
| Supplier Portal | Phase 2 |
| Mobile Application (Flutter) | Phase 3 |
| AI/ML Features | Phase 3 |
| E-Commerce Integration | Phase 3 |
| Multi-Company Support | Phase 3 |
| Offline POS (full sync engine) | Phase 2 (basic offline in MVP) |
| Advanced CRM Segmentation | Phase 3 |
| Franchise Management | Phase 4 |

---

## 2. MVP Technical Scope

### 2.1 Infrastructure (MVP)

| Component | MVP Configuration |
|-----------|------------------|
| Backend | .NET 9 ASP.NET Core, single deployment |
| Database | PostgreSQL 17 (single instance + 1 replica) |
| Cache | Redis (single instance) |
| Queue | RabbitMQ (single instance, durable queues) |
| Search | PostgreSQL full-text search (Elasticsearch deferred) |
| Frontend | React 19 + TypeScript + Ant Design |
| Deployment | Docker Compose (staging), single K8s node (production) |
| CI/CD | GitHub Actions |
| Monitoring | Prometheus + Grafana (basic dashboards) |

### 2.2 Source Code Structure

```
georgia-erp/
├── src/
│   ├── GeorgiaERP.Api/                    # ASP.NET Core Web API
│   │   ├── Controllers/
│   │   ├── Middleware/
│   │   ├── Filters/
│   │   └── Program.cs
│   │
│   ├── GeorgiaERP.Domain/                 # Domain layer (entities, events, interfaces)
│   │   ├── Common/                        # Base entities, value objects
│   │   ├── Products/
│   │   ├── Pricing/
│   │   ├── Inventory/
│   │   ├── Warehouse/
│   │   ├── POS/
│   │   ├── Procurement/
│   │   ├── Compliance/
│   │   ├── Finance/
│   │   └── Identity/
│   │
│   ├── GeorgiaERP.Application/            # Application layer (use cases, DTOs)
│   │   ├── Common/                        # CQRS base, validation, mapping
│   │   ├── Products/
│   │   │   ├── Commands/
│   │   │   ├── Queries/
│   │   │   └── DTOs/
│   │   ├── Pricing/
│   │   ├── Inventory/
│   │   ├── Warehouse/
│   │   ├── POS/
│   │   ├── Procurement/
│   │   ├── Compliance/
│   │   ├── Finance/
│   │   └── Identity/
│   │
│   ├── GeorgiaERP.Infrastructure/         # Infrastructure layer
│   │   ├── Persistence/                   # EF Core DbContext, migrations
│   │   │   ├── Configurations/            # Entity type configurations
│   │   │   ├── Migrations/
│   │   │   └── Repositories/
│   │   ├── RsGe/                          # RS.GE SOAP client
│   │   │   ├── SoapClient/
│   │   │   ├── WaybillService/
│   │   │   ├── InvoiceService/
│   │   │   ├── LookupService/
│   │   │   └── Models/
│   │   ├── Messaging/                     # RabbitMQ integration
│   │   ├── Caching/                       # Redis integration
│   │   ├── Identity/                      # JWT, 2FA
│   │   └── BackgroundJobs/                # Hosted services
│   │
│   └── GeorgiaERP.Workers/               # Background worker service
│       ├── RsGeWorker/                    # RS.GE queue processor
│       ├── ComplianceWorker/              # Deadline monitoring
│       └── SyncWorker/                    # Reference data sync
│
├── src/web/                               # React frontend
│   ├── src/
│   │   ├── app/                           # App shell, routing, providers
│   │   ├── features/                      # Feature-based modules
│   │   │   ├── auth/
│   │   │   ├── products/
│   │   │   ├── pricing/
│   │   │   ├── inventory/
│   │   │   ├── pos/
│   │   │   ├── warehouse/
│   │   │   ├── procurement/
│   │   │   ├── compliance/
│   │   │   ├── finance/
│   │   │   ├── reports/
│   │   │   └── admin/
│   │   ├── shared/                        # Shared components, hooks, utils
│   │   └── i18n/                          # Georgian (ka) + English (en)
│   └── public/
│
├── tests/
│   ├── GeorgiaERP.UnitTests/
│   ├── GeorgiaERP.IntegrationTests/
│   └── GeorgiaERP.E2ETests/
│
├── docker/
│   ├── Dockerfile.api
│   ├── Dockerfile.worker
│   ├── Dockerfile.web
│   └── docker-compose.yml
│
├── k8s/                                   # Kubernetes manifests
│   ├── base/
│   └── overlays/
│       ├── staging/
│       └── production/
│
├── docs/                                  # Architecture documents
│
└── scripts/                               # Build, deploy, seed scripts
```

---

## 3. Development Roadmap

### Phase 1: MVP (Months 1-8)

#### Sprint 1-2 (Weeks 1-4): Foundation
- [ ] Project scaffolding (.NET solution, React app)
- [ ] Database setup (PostgreSQL, EF Core, migrations)
- [ ] Authentication & authorization (JWT, RBAC, 2FA)
- [ ] API Gateway setup (YARP)
- [ ] CI/CD pipeline (GitHub Actions → Docker → staging)
- [ ] Logging & monitoring foundation (Serilog, Prometheus)
- [ ] RS.GE SOAP client foundation (authentication, IP setup)
- [ ] RS.GE reference data sync (units, types, error codes)

#### Sprint 3-4 (Weeks 5-8): Product & Pricing Core
- [ ] Category management (unlimited depth)
- [ ] Product management (CRUD, variants, barcodes)
- [ ] Price list management
- [ ] Basic promotion engine
- [ ] Product search (PostgreSQL full-text)
- [ ] TIN lookup integration (get_name_from_tin, is_vat_payer)
- [ ] Admin UI: Users, stores, warehouses

#### Sprint 5-6 (Weeks 9-12): Inventory & Warehouse
- [ ] Stock level management
- [ ] Goods receiving workflow
- [ ] Stock transfers (with RS.GE waybill creation)
- [ ] Stock adjustments
- [ ] Stock counting
- [ ] Warehouse location management
- [ ] RS.GE waybill full lifecycle (save → send → confirm → close)
- [ ] Waybill retry queue (RabbitMQ)

#### Sprint 7-8 (Weeks 13-16): Procurement
- [ ] Supplier management
- [ ] Purchase order workflow (create → approve → send → receive)
- [ ] Goods receipt notes (linked to waybills)
- [ ] Supplier invoice matching
- [ ] Basic supplier reporting

#### Sprint 9-10 (Weeks 17-20): POS System
- [ ] POS terminal management
- [ ] POS session management (open/close)
- [ ] Sale transaction flow (scan → price → pay)
- [ ] Payment processing (cash, card, mixed)
- [ ] Returns and exchanges
- [ ] Fiscal receipt generation + RS.GE invoice upload
- [ ] Daily closing workflow
- [ ] Receipt printing (thermal printer integration)

#### Sprint 11-12 (Weeks 21-24): Accounting & Compliance
- [ ] Chart of accounts (Georgian standard)
- [ ] Auto journal entries (sales, purchases, transfers)
- [ ] Accounts receivable / payable
- [ ] Bank account management
- [ ] VAT calculation engine
- [ ] Monthly VAT declaration generator
- [ ] RS.GE invoice deadline monitoring
- [ ] Compliance dashboard

#### Sprint 13-14 (Weeks 25-28): Reporting & Polish
- [ ] Sales reports (daily, weekly, monthly, by store)
- [ ] Inventory reports (stock levels, movements, valuation)
- [ ] Financial reports (P&L, balance sheet, trial balance)
- [ ] VAT reports
- [ ] Export (Excel, PDF, CSV)
- [ ] Performance optimization
- [ ] Security hardening
- [ ] UAT preparation

#### Sprint 15-16 (Weeks 29-32): Testing & Deployment
- [ ] RS.GE integration testing (staging environment)
- [ ] End-to-end testing
- [ ] Performance testing (target load)
- [ ] Security audit
- [ ] Production deployment
- [ ] Data migration (if applicable)
- [ ] User training
- [ ] Go-live support

### Phase 2: Enhanced Operations (Months 9-14)

- [ ] CRM & Loyalty Program
- [ ] Advanced Approval Workflows
- [ ] Notification System (SMS via Georgian providers, email)
- [ ] Advanced Reporting & BI Dashboards
- [ ] Offline POS with full sync engine
- [ ] Supplier Portal
- [ ] Bank integration (BoG iPay, TBC Pay)
- [ ] Payment terminal deep integration
- [ ] Scheduled report delivery

### Phase 3: Mobile & Intelligence (Months 15-20)

- [ ] Flutter mobile app (inventory count, receiving, approvals, dashboards)
- [ ] Multi-company support
- [ ] AI demand forecasting
- [ ] Reorder recommendations
- [ ] Sales trend analysis
- [ ] Elasticsearch migration (product search)
- [ ] E-Commerce platform integration
- [ ] Customer-facing features

### Phase 4: Scale & Expand (Months 21+)

- [ ] Franchise management
- [ ] Supplier portal
- [ ] Customer portal / mobile app
- [ ] B2B sales portal
- [ ] Marketplace integrations
- [ ] Advanced BI platform
- [ ] Natural language reporting
- [ ] Fraud detection

---

## 4. Team Structure (Recommended)

### MVP Team (Phase 1)

| Role | Count | Responsibilities |
|------|-------|-----------------|
| Tech Lead / Architect | 1 | Architecture decisions, RS.GE integration design, code review |
| Senior Backend Developer | 2 | .NET core modules, SOAP integration, database |
| Mid Backend Developer | 1 | API development, business logic |
| Senior Frontend Developer | 1 | React architecture, POS UI, admin UI |
| Mid Frontend Developer | 1 | Feature development, i18n |
| QA Engineer | 1 | Test strategy, automation, RS.GE testing |
| DevOps Engineer | 1 (part-time) | CI/CD, infrastructure, monitoring |
| **Total** | **7-8** | |

### Scaling for Phase 2+

| Phase | Additional Roles |
|-------|-----------------|
| Phase 2 | +1 Backend, +1 Frontend, +1 QA |
| Phase 3 | +2 Flutter developers, +1 ML Engineer |
| Phase 4 | Scale based on specific requirements |

---

## 5. Cost Estimation

### 5.1 Infrastructure Costs (Monthly, Production)

| Component | Provider | Estimated Cost/Month |
|-----------|----------|---------------------|
| Kubernetes Cluster (3 nodes) | AWS EKS / Azure AKS | $300-500 |
| PostgreSQL (db.r6g.xlarge + replica) | AWS RDS / Azure DB | $400-600 |
| Redis (cache.r6g.large) | AWS ElastiCache | $100-150 |
| RabbitMQ (managed or self-hosted) | Self-hosted on K8s | Included in K8s |
| Static IP (for RS.GE) | AWS/Azure | $5-10 |
| Object Storage (backups, documents) | S3/Blob | $50-100 |
| Monitoring (Prometheus/Grafana) | Self-hosted on K8s | Included in K8s |
| Domain + SSL | Cloudflare | $20-50 |
| **Total Infrastructure** | | **$875-1,410/month** |

### 5.2 Development Cost Estimate

| Phase | Duration | Team Size | Estimated Cost |
|-------|----------|-----------|---------------|
| Phase 1 (MVP) | 8 months | 7-8 people | $280,000 - $480,000 |
| Phase 2 | 6 months | 9-10 people | $270,000 - $450,000 |
| Phase 3 | 6 months | 11-12 people | $330,000 - $540,000 |
| Phase 4 | Ongoing | Variable | Variable |

*Estimates based on mid-market Eastern European developer rates. Adjust for Georgian local market rates which may be 30-50% lower.*

---

## 6. Success Criteria for MVP

| Criterion | Target | Measurement |
|-----------|--------|-------------|
| POS transaction speed | < 2 seconds end-to-end | Automated performance test |
| RS.GE waybill success rate | > 99% (excluding RS.GE downtime) | Monitoring dashboard |
| RS.GE invoice upload compliance | 100% within 30-day deadline | Deadline monitoring |
| System uptime | > 99.5% (MVP target) | Uptime monitoring |
| Daily closing accuracy | 100% balance | Reconciliation report |
| Concurrent cashier support | 50+ (MVP) | Load test |
| Product catalog | 100,000+ products | Database count |
| Data integrity | Zero lost transactions | Audit log verification |

---

## 7. Key Risks to Timeline

| Risk | Impact | Mitigation |
|------|--------|------------|
| RS.GE API documentation gaps | High | Early prototype, contact RS.GE support, community resources |
| RS.GE testing environment access | High | Request staging access early, build mock service |
| SOAP complexity in .NET 9 | Medium | Evaluate WCF Core vs HttpClient approach early |
| Payment terminal integration | Medium | Identify terminal provider early, get SDK |
| Georgian localization complexity | Medium | Involve Georgian-speaking team member from start |
| Receipt printer compatibility | Low-Medium | Test with common Georgian retail printers |
| Regulatory changes during development | Low | Configuration-driven compliance rules |

---

## 8. Definition of Done (Architecture Approval Gate)

Before proceeding to code implementation, the following must be approved:

- [x] Business Analysis Document
- [x] Solution Architecture Document
- [x] Database Design Document
- [x] RS.GE Technical Analysis & Compliance Risk Assessment
- [x] MVP Definition & Development Roadmap (this document)
- [ ] **Stakeholder sign-off on architecture**
- [ ] **Stakeholder sign-off on MVP scope**
- [ ] **Stakeholder sign-off on technology stack**
- [ ] **Stakeholder sign-off on timeline and budget**

**Once all sign-offs are received, Sprint 1 begins.**
