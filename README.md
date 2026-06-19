# Enterprise Retail ERP Platform for Georgia

An enterprise-grade Retail ERP platform designed for the Georgian market, built around RS.GE (Georgian Revenue Service) compliance as a core architectural principle.

## Project Status

**Phase: Implementation** — backend (8 modules), WPF desktop client, licensing, RS.GE compliance pipeline, Docker deployment, and an automated test suite are in place.

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
- **Database**: PostgreSQL 17 with schema-per-tenant multi-tenancy
- **Frontend**: React 19 + TypeScript + Ant Design
- **Mobile**: Flutter (Phase 3)
- **Queue**: RabbitMQ for reliable RS.GE message delivery
- **Cache**: Redis for sessions and reference data

## Technology Stack

| Layer | Technology |
|-------|-----------|
| Backend | .NET 9, ASP.NET Core, Entity Framework Core |
| Desktop client | WPF (.NET 9), MVVM (CommunityToolkit.Mvvm) |
| Database | PostgreSQL 16 |
| Cache | Redis 7 |
| Queue | RabbitMQ 4 |
| Mobile | Flutter 3 (Phase 3) |
| Infrastructure | Docker, Kubernetes, AWS/Azure |
| CI/CD | GitHub Actions |
| Monitoring | Prometheus, Grafana, Seq |

## RS.GE Compliance

The platform integrates with Georgian Revenue Service through SOAP web services:

- **Electronic Waybills**: Full lifecycle management (create → send → confirm → close)
- **Invoice Upload**: Automated upload with 30-day deadline tracking
- **VAT Engine**: 18% Georgian VAT calculation and monthly declaration
- **TIN Validation**: Real-time taxpayer identification verification
- **Audit Trail**: Complete request/response logging for all RS.GE communications
- **Retry Queue**: Automatic retry with exponential backoff for failed submissions

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
Default admin credentials: `admin` / `Admin@123!`

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
