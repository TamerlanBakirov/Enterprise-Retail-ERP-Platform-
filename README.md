# Enterprise Retail ERP Platform for Georgia

An enterprise-grade Retail ERP platform designed for the Georgian market, built around RS.GE (Georgian Revenue Service) compliance as a core architectural principle.

## Project Status

**Phase: Architecture & Business Analysis** (Awaiting stakeholder approval before implementation begins)

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
| Frontend | React 19, TypeScript, Ant Design |
| Database | PostgreSQL 17 |
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

> Implementation has not yet begun. This repository currently contains architecture documentation only. See the [MVP Definition & Roadmap](docs/05-MVP-DEFINITION-AND-ROADMAP.md) for the planned implementation timeline.

## License

Proprietary — All rights reserved.
