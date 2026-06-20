# Enterprise Retail ERP Platform — Agent Automation Rules

## Tech Stack
- Backend: .NET 9, ASP.NET Core, Entity Framework Core
- Desktop: WPF (.NET 9), CommunityToolkit.Mvvm
- Database: PostgreSQL 16
- Queue: RabbitMQ 4
- Cache: Redis 7
- CI/CD: GitHub Actions
- Infrastructure: Docker Compose

## Auto-Invoke Agent Rules

These agents MUST be invoked automatically at the described trigger points. Do NOT ask for permission — run them directly.

### On Every Code Change (src/)
- **@code-reviewer** — Run after completing any feature, bugfix, or refactor in `src/`. Review all changed files before commit.

### On Security-Sensitive Changes
- **@security-auditor** — Auto-run when ANY of these are touched:
  - Authentication/Authorization code (`*Auth*`, `*Token*`, `*Jwt*`, `*Permission*`, `*Role*`)
  - User data handling (`*User*`, `*Password*`, `*Credential*`)
  - API endpoints (`*Controller*`, `*Endpoint*`)
  - Configuration files (`appsettings*.json`, `docker-compose.yml`)
  - RS.GE integration (`*RsGe*`, `*Waybill*`, `*Invoice*`)

### On Database Changes
- **@database-planner** — Auto-run when schema/models change (`*Entity*`, `*Migration*`, `*DbContext*`)
- **@sql-expert** — Auto-run when queries, indexes, or migrations are added/modified

### On Test Changes
- **@test-strategist** — Auto-run when:
  - New feature code is written without corresponding tests
  - Test files are created or modified
  - Before any commit that includes `src/` changes

### On Architecture Decisions
- **@system-designer** — Auto-run when new modules, services, or major abstractions are introduced
- **@api-designer** — Auto-run when API contracts change (new controllers, DTOs, endpoints)
- **@solution-architect** — Auto-run when RS.GE integration patterns or cross-module communication changes

### On Infrastructure Changes
- **@deployment-troubleshooter** — Auto-run when `Dockerfile*`, `docker-compose.yml`, or CI/CD workflows change
- **@monitoring-setup** — Auto-run when logging, metrics, or health check code changes
- **@backup-planner** — Auto-run when data retention, migration, or storage strategies change

### On Code Quality
- **@performance-optimizer** — Auto-run when:
  - Database queries are written or modified
  - Queue processing code changes
  - API endpoint response paths are modified
- **@refactoring-expert** — Auto-run when code duplication is detected or module boundaries are unclear

### On Documentation
- **@documentation-writer** — Auto-run when public API surface changes or new modules are added
- **@feature-spec-writer** — Auto-run when new features are planned or user stories are discussed

## Agent Execution Order (Pre-Commit)
1. @code-reviewer (all changed files)
2. @security-auditor (if security-sensitive files changed)
3. @test-strategist (verify test coverage)
4. @sql-expert (if DB changes)
5. @performance-optimizer (if query/API changes)

## Project Conventions
- Clean Architecture: Domain → Application → Infrastructure → Api
- All RS.GE communication goes through queue (RabbitMQ)
- JWT + 2FA authentication
- RBAC authorization
- Full audit trail for RS.GE operations
- Georgian locale support (ka-GE)
