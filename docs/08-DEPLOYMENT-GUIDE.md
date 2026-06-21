# Deployment Guide

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Last Updated:** June 2026

---

## Table of Contents

1. [Prerequisites](#1-prerequisites)
2. [Architecture Overview](#2-architecture-overview)
3. [Environment Variables](#3-environment-variables)
4. [Local Development Setup](#4-local-development-setup)
5. [Docker Compose Deployment](#5-docker-compose-deployment)
6. [Production Deployment](#6-production-deployment)
7. [Database Migrations](#7-database-migrations)
8. [Health Checks and Monitoring](#8-health-checks-and-monitoring)
9. [SSL/TLS Configuration](#9-ssltls-configuration)
10. [Backup and Recovery](#10-backup-and-recovery)
11. [Production Checklist](#11-production-checklist)

---

## 1. Prerequisites

### Required Software

| Software    | Version  | Purpose                            |
|------------|----------|------------------------------------|
| .NET SDK   | 9.0.x    | Build and run the application      |
| PostgreSQL | 16+      | Primary database                   |
| RabbitMQ   | 3.13+    | Message queue for RS.GE async operations |
| Docker     | 24+      | Container runtime (optional)       |
| Docker Compose | 2.20+ | Multi-container orchestration    |
| Git        | 2.40+    | Source control                     |

### Hardware Requirements

**Development:**
- 4 GB RAM minimum
- 2 CPU cores
- 10 GB disk space

**Production (minimum):**
- 8 GB RAM
- 4 CPU cores
- 50 GB SSD storage
- Network access to RS.GE SOAP services (waybill.rs.ge)

---

## 2. Architecture Overview

The platform consists of four services:

```
+-------------------+     +-------------------+
|   georgia-erp-api |     | georgia-erp-workers|
|   (ASP.NET Core)  |     | (Background svc)   |
|   Port: 5000      |     |                     |
+--------+----------+     +--------+------------+
         |                          |
         v                          v
+--------+----------+     +--------+------------+
|  georgia-erp-db   |     | georgia-erp-mq      |
|  (PostgreSQL 16)  |     | (RabbitMQ 3.13)      |
|  Port: 5432       |     | Port: 5672 (AMQP)    |
|                   |     | Port: 15672 (Mgmt)   |
+-------------------+     +----------------------+
```

| Service  | Container Name       | Description                                |
|----------|---------------------|--------------------------------------------|
| API      | georgia-erp-api     | ASP.NET Core 9 REST API, 50+ endpoints     |
| Workers  | georgia-erp-workers | Background service for RS.GE SOAP operations|
| Database | georgia-erp-db      | PostgreSQL 16 with ERP schema              |
| Queue    | georgia-erp-mq      | RabbitMQ for async waybill processing      |

---

## 3. Environment Variables

### API Service

| Variable | Description | Default / Example | Required |
|----------|-------------|-------------------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | `Host=postgres;Database=georgia_erp;Username=erp_user;Password=erp_dev_password` | Yes |
| `Jwt__SecretKey` | JWT signing key (min 32 chars) | `dev-secret-key-change-in-production-min-32-chars!` | Yes |
| `Jwt__Issuer` | JWT issuer claim | `GeorgiaERP` | Yes |
| `Jwt__Audience` | JWT audience claim | `GeorgiaERP.Client` | Yes |
| `RsGe__Queue__HostName` | RabbitMQ host | `rabbitmq` | Yes |
| `RsGe__Queue__UserName` | RabbitMQ username | `erp_user` | Yes |
| `RsGe__Queue__Password` | RabbitMQ password | `erp_dev_password` | Yes |
| `Licensing__SigningKey` | License validation signing key (64 chars) | See docker-compose | Yes |
| `ASPNETCORE_URLS` | Listening URL | `http://+:5000` | Yes |
| `ASPNETCORE_ENVIRONMENT` | Runtime environment | `Development` / `Production` | Yes |
| `Cors__Origins__0` | Allowed CORS origin | `http://localhost:3000` | No |
| `Swagger__Enabled` | Enable Swagger UI | `true` / `false` | No |
| `Seed__Demo` | Seed demo data on startup | `true` / `false` | No |
| `App__LatestVersion` | Latest app version for update checks | `1.0.0` | No |
| `App__DownloadUrl` | Download URL for updates | URL | No |
| `App__ReleaseNotes` | Release notes text | Text | No |
| `App__Sha256` | SHA256 hash of download | Hash | No |

### Workers Service

| Variable | Description | Default / Example | Required |
|----------|-------------|-------------------|----------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | Same as API | Yes |
| `RsGe__Queue__HostName` | RabbitMQ host | `rabbitmq` | Yes |
| `RsGe__Queue__UserName` | RabbitMQ username | `erp_user` | Yes |
| `RsGe__Queue__Password` | RabbitMQ password | `erp_dev_password` | Yes |

### PostgreSQL

| Variable | Description | Default |
|----------|-------------|---------|
| `POSTGRES_DB` | Database name | `georgia_erp` |
| `POSTGRES_USER` | Database user | `erp_user` |
| `POSTGRES_PASSWORD` | Database password | `erp_dev_password` |

### RabbitMQ

| Variable | Description | Default |
|----------|-------------|---------|
| `RABBITMQ_DEFAULT_USER` | RabbitMQ username | `erp_user` |
| `RABBITMQ_DEFAULT_PASS` | RabbitMQ password | `erp_dev_password` |

---

## 4. Local Development Setup

### Option A: Docker Compose (Recommended)

The fastest way to get a full development environment running.

**Step 1: Clone the repository**

```bash
git clone https://github.com/your-org/Enterprise-Retail-ERP-Platform-.git
cd Enterprise-Retail-ERP-Platform-
```

**Step 2: Start all services**

```bash
docker compose up -d
```

This starts PostgreSQL, RabbitMQ, the API, and the workers service. The API automatically runs database migrations and seeds development data on startup.

**Step 3: Verify services are running**

```bash
# Check all containers are up
docker compose ps

# Test the API health endpoint
curl http://localhost:5000/health

# Access Swagger UI
# Open in browser: http://localhost:5000/swagger
```

**Step 4: Access RabbitMQ Management UI**

Open `http://localhost:15672` in your browser. Login with `erp_user` / `erp_dev_password`.

---

### Option B: Native Development

Run .NET directly on your machine with Docker only for infrastructure services.

**Step 1: Start infrastructure services**

```bash
docker compose up -d postgres rabbitmq
```

**Step 2: Configure the API**

The API uses `appsettings.Development.json` for local config. Verify the connection string points to `localhost:5432`.

**Step 3: Run the API**

```bash
cd src/GeorgiaERP.Api
dotnet run
```

The API starts on `http://localhost:5000` by default. In development mode, it automatically:
- Runs database migrations via EF Core
- Seeds initial data (roles, admin user, demo data)
- Enables Swagger UI

**Step 4: Run the Workers**

In a separate terminal:

```bash
cd src/GeorgiaERP.Workers
dotnet run
```

---

## 5. Docker Compose Deployment

### Root docker-compose.yml

The root `docker-compose.yml` provides a complete deployment with all four services.

**Services and Ports:**

| Service   | Internal Port | External Port | Purpose                |
|-----------|--------------|---------------|------------------------|
| postgres  | 5432         | 5432          | Database               |
| rabbitmq  | 5672         | 5672          | AMQP message queue     |
| rabbitmq  | 15672        | 15672         | RabbitMQ management UI |
| api       | 5000         | 5000          | REST API               |

**Health Checks:**

All services include health checks that Docker Compose uses to manage startup ordering:

- **PostgreSQL:** `pg_isready -U erp_user -d georgia_erp` (every 5s, 3s timeout, 5 retries)
- **RabbitMQ:** `rabbitmq-diagnostics -q ping` (every 10s, 5s timeout, 5 retries)
- **API:** Depends on postgres + rabbitmq being healthy
- **Workers:** Depends on postgres + rabbitmq being healthy

**Persistent Volumes:**

| Volume         | Purpose                    |
|---------------|----------------------------|
| postgres_data | PostgreSQL data directory   |
| rabbitmq_data | RabbitMQ messages and config|

### Docker Build Process

Both the API and Workers use multi-stage Docker builds:

**API (Dockerfile):**

```
Stage 1 (build): .NET SDK 9.0
  - Restore NuGet packages
  - Publish Release build
Stage 2 (runtime): .NET ASP.NET Runtime 9.0
  - Copy published output
  - Expose port 5000
  - Entry point: dotnet GeorgiaERP.Api.dll
```

**Workers (Dockerfile.workers):**

```
Stage 1 (build): .NET SDK 9.0
  - Restore NuGet packages
  - Publish Release build
Stage 2 (runtime): .NET ASP.NET Runtime 9.0
  - Copy published output
  - Entry point: dotnet GeorgiaERP.Workers.dll
```

### Starting and Stopping

```bash
# Start all services
docker compose up -d

# View logs
docker compose logs -f api
docker compose logs -f workers

# Stop all services
docker compose down

# Stop and remove volumes (destroys data)
docker compose down -v

# Rebuild after code changes
docker compose up -d --build
```

---

## 6. Production Deployment

### Pre-Deployment Steps

1. **Generate secure secrets:**

   ```bash
   # Generate JWT secret (min 32 characters)
   openssl rand -base64 48

   # Generate licensing signing key (64 characters)
   openssl rand -base64 48
   ```

2. **Create a production docker-compose override:**

   Create `docker-compose.prod.yml`:

   ```yaml
   services:
     postgres:
       environment:
         POSTGRES_PASSWORD: <strong-generated-password>
       volumes:
         - /data/postgres:/var/lib/postgresql/data

     rabbitmq:
       environment:
         RABBITMQ_DEFAULT_PASS: <strong-generated-password>
       ports:
         # Do NOT expose management UI in production
         - "5672:5672"

     api:
       environment:
         ConnectionStrings__DefaultConnection: "Host=postgres;Database=georgia_erp;Username=erp_user;Password=<strong-password>"
         Jwt__SecretKey: "<generated-jwt-secret>"
         Licensing__SigningKey: "<generated-license-key>"
         ASPNETCORE_ENVIRONMENT: Production
         Swagger__Enabled: "false"
       ports:
         - "5000:5000"
       restart: unless-stopped
       deploy:
         resources:
           limits:
             memory: 2G
             cpus: '2'

     workers:
       environment:
         ConnectionStrings__DefaultConnection: "Host=postgres;Database=georgia_erp;Username=erp_user;Password=<strong-password>"
       restart: unless-stopped
       deploy:
         resources:
           limits:
             memory: 1G
             cpus: '1'
   ```

3. **Deploy with production overrides:**

   ```bash
   docker compose -f docker-compose.yml -f docker-compose.prod.yml up -d
   ```

### Reverse Proxy Setup (Nginx)

In production, place an Nginx reverse proxy in front of the API:

```nginx
server {
    listen 443 ssl http2;
    server_name erp.yourcompany.ge;

    ssl_certificate     /etc/ssl/certs/erp.yourcompany.ge.crt;
    ssl_certificate_key /etc/ssl/private/erp.yourcompany.ge.key;

    location / {
        proxy_pass http://localhost:5000;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /health {
        proxy_pass http://localhost:5000/health;
        access_log off;
    }
}
```

---

## 7. Database Migrations

### Automatic Migrations

In **Development** mode, the API applies pending EF Core migrations automatically on startup. Database seeding also runs automatically, creating:
- Default roles (super_admin, company_admin, store_manager, cashier, accountant, warehouse_manager)
- Admin user
- Demo data (if `--seed-demo` flag or `Seed:Demo = true`)

### Manual Migrations

For production environments, apply migrations manually:

**Using dotnet CLI:**

```bash
cd src/GeorgiaERP.Api

# Apply pending migrations
dotnet ef database update --connection "Host=<host>;Database=georgia_erp;Username=erp_user;Password=<password>"

# Generate SQL script for review before applying
dotnet ef migrations script --idempotent -o migrations.sql
```

**Using Docker exec:**

```bash
# Connect to the running API container and run migrations
docker exec -it georgia-erp-api dotnet ef database update
```

### Creating New Migrations

When schema changes are made:

```bash
cd src/GeorgiaERP.Api

# Create a new migration
dotnet ef migrations add <MigrationName> \
  --project ../GeorgiaERP.Infrastructure \
  --startup-project .
```

### Database Seeding

```bash
# Seed required data (roles, admin user)
dotnet run --project src/GeorgiaERP.Api -- --seed

# Seed demo data (products, customers, etc.)
dotnet run --project src/GeorgiaERP.Api -- --seed-demo
```

---

## 8. Health Checks and Monitoring

### Health Check Endpoint

```
GET /health
```

Returns `Healthy` or `Unhealthy` based on database connectivity.

**Example monitoring script:**

```bash
#!/bin/bash
STATUS=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health)
if [ "$STATUS" != "200" ]; then
  echo "ALERT: ERP API health check failed with status $STATUS"
  # Send alert notification
fi
```

### RS.GE Integration Health

```
GET /api/v1/compliance/rsge/health
```

Returns RS.GE service connectivity status (`Connected` or `Unavailable`).

### Logging

The API uses Serilog with two sinks:

| Sink    | Output                           | Configuration                |
|---------|----------------------------------|------------------------------|
| Console | stdout (captured by Docker logs) | Always enabled               |
| File    | `logs/georgia-erp-YYYYMMDD.log`  | Rolling daily, 14 day retention |

**Log format:**

```
2026-06-20 12:00:00.123 +04:00 [INF] Georgia ERP Platform starting up...
2026-06-20 12:00:01.456 +04:00 [INF] HTTP GET /health responded 200 in 12ms
```

**Viewing container logs:**

```bash
# Follow API logs
docker logs -f georgia-erp-api

# Follow worker logs
docker logs -f georgia-erp-workers

# Show last 100 lines
docker logs --tail 100 georgia-erp-api
```

### RabbitMQ Monitoring

- **Management UI:** `http://localhost:15672` (dev only)
- **Queue metrics:** Monitor the RS.GE waybill queue depth and consumer status
- **Dead letter queues:** Check for failed message processing

---

## 9. SSL/TLS Configuration

### Development

Not required. The API runs on HTTP (`http://+:5000`).

### Production

**Option 1: Terminate TLS at the reverse proxy** (recommended)

Use Nginx or a load balancer to handle SSL. The API runs on HTTP internally. Set the `X-Forwarded-Proto` header so the API knows the original scheme.

**Option 2: Kestrel TLS**

Set environment variables:

```yaml
environment:
  ASPNETCORE_URLS: "https://+:5001;http://+:5000"
  ASPNETCORE_Kestrel__Certificates__Default__Path: /certs/erp.pfx
  ASPNETCORE_Kestrel__Certificates__Default__Password: cert-password
```

Mount the certificate volume:

```yaml
volumes:
  - ./certs:/certs:ro
```

---

## 10. Backup and Recovery

### Database Backup

**Automated backup script:**

```bash
#!/bin/bash
BACKUP_DIR="/backups/postgres"
TIMESTAMP=$(date +%Y%m%d_%H%M%S)
FILENAME="georgia_erp_${TIMESTAMP}.sql.gz"

docker exec georgia-erp-db pg_dump -U erp_user georgia_erp | gzip > "${BACKUP_DIR}/${FILENAME}"

# Keep only last 30 days of backups
find "${BACKUP_DIR}" -name "*.sql.gz" -mtime +30 -delete

echo "Backup completed: ${FILENAME}"
```

**Restore from backup:**

```bash
gunzip < /backups/postgres/georgia_erp_20260620_120000.sql.gz | \
  docker exec -i georgia-erp-db psql -U erp_user georgia_erp
```

### RabbitMQ Backup

RabbitMQ definitions (exchanges, queues, bindings) can be exported via the management API:

```bash
curl -u erp_user:erp_dev_password \
  http://localhost:15672/api/definitions > rabbitmq_definitions.json
```

Restore:

```bash
curl -u erp_user:erp_dev_password \
  -X POST -H "Content-Type: application/json" \
  -d @rabbitmq_definitions.json \
  http://localhost:15672/api/definitions
```

---

## 11. Production Checklist

### Security

- [ ] Change all default passwords (PostgreSQL, RabbitMQ, JWT secret, licensing key)
- [ ] Generate strong JWT secret (minimum 32 characters, cryptographically random)
- [ ] Generate strong licensing signing key (64 characters)
- [ ] Set `ASPNETCORE_ENVIRONMENT` to `Production`
- [ ] Disable Swagger UI (`Swagger__Enabled=false`)
- [ ] Configure CORS origins to match production frontend URL only
- [ ] Do NOT expose RabbitMQ management port (15672) externally
- [ ] Do NOT expose PostgreSQL port (5432) externally
- [ ] Enable HTTPS (TLS termination at proxy or Kestrel)
- [ ] Set HSTS headers (automatic in production mode)

### Infrastructure

- [ ] Use managed PostgreSQL service or configure replication
- [ ] Configure persistent volumes on reliable storage (SSD recommended)
- [ ] Set up automated database backups (daily minimum)
- [ ] Configure container restart policies (`restart: unless-stopped`)
- [ ] Set resource limits on containers (CPU and memory)
- [ ] Set up a reverse proxy (Nginx/Caddy) with TLS
- [ ] Configure DNS for the API domain

### Monitoring

- [ ] Set up health check monitoring (`/health` endpoint)
- [ ] Set up RS.GE connectivity monitoring (`/api/v1/compliance/rsge/health`)
- [ ] Configure log aggregation (ELK, Datadog, etc.)
- [ ] Monitor RabbitMQ queue depth for RS.GE operations
- [ ] Set up alerting for failed health checks
- [ ] Monitor disk space for log files and database

### Application

- [ ] Run database migrations before deploying new code
- [ ] Seed required data (roles, admin user) on first deployment
- [ ] Configure RS.GE SOAP credentials for production environment
- [ ] Activate the license with a production license key
- [ ] Verify fiscal document submission to RS.GE works
- [ ] Test waybill creation and confirmation end-to-end

### CI/CD

- [ ] CI pipeline builds and tests on push to `main` and `claude/**` branches
- [ ] CI runs on ubuntu-latest with PostgreSQL service container
- [ ] Desktop build runs on windows-latest
- [ ] Test results are uploaded as artifacts
- [ ] Consider adding deployment stages (staging, production)
