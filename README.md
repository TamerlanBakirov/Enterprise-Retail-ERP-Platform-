# Enterprise Retail ERP Platform for Georgia

An enterprise-grade Retail ERP platform designed for the Georgian market, built around RS.GE (Georgian Revenue Service) compliance as a core architectural principle.

## Project Status

**Phase: Implementation** - backend modules, WPF desktop client, signed licensing, RS.GE queue processing, Docker Compose, and automated tests are in place. External certification and production validation remain outstanding.

## Architecture Documents

| # | Document | Description |
|---|----------|-------------|
| 1 | [Business Analysis](docs/01-BUSINESS-ANALYSIS.md) | Market analysis, regulatory environment, RS.GE API catalog, stakeholder analysis, business processes, functional & non-functional requirements |
| 2 | [Solution Architecture](docs/02-SOLUTION-ARCHITECTURE.md) | Modular monolith architecture, module boundaries, compliance layer design, API architecture, security model, offline POS, infrastructure & deployment |
| 3 | [Database Design](docs/03-DATABASE-DESIGN.md) | PostgreSQL schema design, table definitions, indexing strategy, partitioning, data retention policy |
| 4 | [RS.GE Technical Analysis](docs/04-RSGE-TECHNICAL-ANALYSIS.md) | Complete RS.GE API analysis (60+ operations), waybill lifecycle, invoice requirements, VAT engine, compliance risk matrix |
| 5 | [MVP Definition & Roadmap](docs/05-MVP-DEFINITION-AND-ROADMAP.md) | MVP scope, source code structure, sprint plan, team structure, cost estimation, success criteria |
| 6 | [Security Architecture](docs/06-SECURITY-ARCHITECTURE.md) | Authentication (JWT + 2FA), RBAC, data encryption, network security, audit logging, GDPR compliance |

## Key Architecture Decisions

- **Architecture Style**: Modular Monolith with event-driven internals
- **RS.GE Integration**: Queue-based SOAP communication with retry logic and full audit trail
- **Backend**: .NET 9 (ASP.NET Core) — chosen for native SOAP support critical for RS.GE
- **Database**: PostgreSQL with a single application schema (multi-company isolation is deferred)
- **Frontend**: WPF desktop client using CommunityToolkit.Mvvm
- **Mobile**: Flutter (Phase 3)
- **Queue**: RabbitMQ for reliable RS.GE message delivery
- **Cache**: Redis is available in the development Compose stack; application caching is deferred

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 9, ASP.NET Core, Entity Framework Core |
| Desktop client | WPF (.NET 9), MVVM (CommunityToolkit.Mvvm) |
| Database | PostgreSQL 16 |
| Cache | Redis 7 (planned application integration) |
| Queue | RabbitMQ 4 |
| Mobile | Flutter 3 (Phase 3) |
| Infrastructure | Docker Compose; Kubernetes/cloud deployment is planned |
| CI/CD | GitHub Actions |
| Monitoring | Serilog file/console logging; metrics dashboards are planned |

## RS.GE Compliance

The platform integrates with Georgian Revenue Service through SOAP web services:

- **Electronic Waybills**: Full lifecycle management (create → send → confirm → close)
- **Invoice Upload**: Queue-based upload with 30-day deadline tracking and at-risk API reporting
- **VAT Engine**: 18% Georgian VAT calculation and monthly declaration
- **TIN Validation**: Real-time taxpayer identification verification
- **Audit Trail**: Complete request/response logging for all RS.GE communications
- **Retry Queue**: Automatic retry with exponential backoff for failed submissions

## Production Readiness

The repository builds and its automated suite covers domain behavior, API authentication/authorization, licensing, and selected workflows. Production launch still requires work that depends on the target environment:

- RS.GE staging credentials, fixed-IP registration, conformance testing, and sign-off
- Integration tests against real PostgreSQL, RabbitMQ, and RS.GE staging services
- Load, penetration, backup-restore, disaster-recovery, and user-acceptance tests
- Fiscal/thermal printer, payment-terminal, bank, and local accounting acceptance testing
- Production secrets, TLS certificates, monitoring/alerting, and an operator runbook

## Getting Started

### Prerequisites
- .NET 9 SDK
- PostgreSQL 16 and RabbitMQ 3.13 (or use Docker Compose, below)

### Run with Docker Compose
```bash
docker compose up --build
```
This starts PostgreSQL, RabbitMQ, the API, and the background workers. The API runs on port 5000.

### Run the API locally
```bash
# Applies migrations and seeds roles + admin user automatically in Development.
dotnet run --project src/GeorgiaERP.Api
# Optionally load a realistic demo dataset (company, store, products, stock, open POS session):
dotnet run --project src/GeorgiaERP.Api -- --seed-demo
```
Development-only admin credentials: `admin` / `Admin@123!`. Change or disable this account outside local development.

### API documentation
Swagger UI is served at `/swagger` in Development (and when `Swagger:Enabled=true`).

### Desktop client (Windows)
```bash
dotnet build src/GeorgiaERP.Desktop   # requires Windows (WPF / net9.0-windows)
```
The installer is built from `installer/GeorgiaERP.iss` (Inno Setup).

### Tests
```bash
dotnet test tests/GeorgiaERP.Tests
```

### Database backup
```bash
PGPASSWORD=... ./scripts/backup-db.sh   # compressed pg_dump with rolling retention
```

### Continuous integration
GitHub Actions (`.github/workflows/ci.yml`) builds the server projects + runs tests on Linux (with a PostgreSQL service), and builds the WPF desktop client on Windows.

## License

Proprietary — All rights reserved.
