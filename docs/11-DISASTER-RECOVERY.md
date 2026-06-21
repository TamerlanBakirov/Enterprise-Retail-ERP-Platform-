# Disaster Recovery Plan

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 2026
**Classification:** Internal -- Restricted
**Owner:** Platform Operations Team
**Review Cycle:** Quarterly (minimum), after every DR drill, and after any major incident

---

## 1. Overview

This document defines the Disaster Recovery (DR) plan for the Georgia Enterprise Retail ERP Platform. The platform processes financial transactions, manages inventory, and interfaces with the Georgian Revenue Service (RS.GE) for tax compliance. Any extended outage has direct financial and regulatory impact.

### 1.1 Scope

This DR plan covers:

- PostgreSQL 16 database (primary data store)
- RabbitMQ message broker (RS.GE integration queue)
- .NET 9 API and Worker services
- Docker Compose infrastructure
- Network and DNS configuration

### 1.2 Critical System Dependencies

| Component | Role | Data Sensitivity |
|-----------|------|-----------------|
| PostgreSQL 16 | Financial records, inventory, customers, audit trails | Critical -- regulatory retention |
| RabbitMQ | RS.GE invoice/waybill queue, async processing | High -- in-flight fiscal documents |
| Redis | Session cache, temporary data | Low -- reconstructable |
| API Service | Business logic, REST endpoints | N/A -- stateless, code in Git |
| Worker Service | Background job processing | N/A -- stateless, code in Git |
| RS.GE Integration | Georgian Revenue Service SOAP interface | External dependency |

---

## 2. Recovery Objectives

### 2.1 Recovery Point Objective (RPO)

The RPO defines the maximum acceptable data loss measured in time.

| Tier | RPO Target | Components | Justification |
|------|-----------|------------|---------------|
| Tier 1 | 1 hour | PostgreSQL (financial transactions, RS.GE records) | Regulatory compliance: RS.GE submissions must be recoverable; financial records require minimal loss |
| Tier 2 | 4 hours | PostgreSQL (inventory, product catalog) | Business operations can reconstruct from physical inventory counts |
| Tier 3 | 24 hours | RabbitMQ definitions, Redis data | Queue definitions change infrequently; Redis is a cache |

**Current backup strategy achieves:**
- Daily pg_dump backups at 2:00 AM = 24-hour RPO for logical backups
- To achieve 1-hour RPO: enable PostgreSQL WAL archiving for continuous archival (see Section 7.1)

### 2.2 Recovery Time Objective (RTO)

The RTO defines the maximum acceptable downtime.

| Scenario | RTO Target | Notes |
|----------|-----------|-------|
| Single service failure (API/Worker) | 5 minutes | Container auto-restart via Docker |
| Database failure (data intact) | 15 minutes | Restart container, verify data |
| Database failure (data lost) | 1 hour | Restore from backup, validate |
| RabbitMQ failure | 30 minutes | Restore definitions, replay pending |
| Complete site failure | 4 hours | Full infrastructure rebuild + restore |
| Data center migration | 8 hours | Planned, with maintenance window |

---

## 3. Backup Strategy Summary

### 3.1 Automated Backup Schedule

| Backup Type | Schedule | Retention | Script |
|-------------|----------|-----------|--------|
| PostgreSQL daily | 2:00 AM daily | 7 days | `scripts/backup/pg_backup.sh --type daily` |
| PostgreSQL weekly | 3:00 AM Sundays | 4 weeks | `scripts/backup/pg_backup.sh --type weekly` |
| PostgreSQL monthly | 4:00 AM 1st of month | 12 months | `scripts/backup/pg_backup.sh --type monthly` |
| RabbitMQ definitions | 2:30 AM daily | 30 days | `scripts/backup/rabbitmq_backup.sh` |
| Docker volumes | 5:00 AM Sundays | 14 days | `scripts/backup/docker_volumes_backup.sh` |

### 3.2 Backup Storage Locations

| Location | Purpose | Encryption |
|----------|---------|-----------|
| Local disk | Fast recovery, primary backups | At-rest (filesystem encryption) |
| S3-compatible remote | Off-site DR, geographic redundancy | AES-256 (S3 server-side) |

### 3.3 Backup Verification

- Every backup is verified with `pg_restore --list` immediately after creation
- SHA256 checksums are generated and stored alongside each backup
- Monthly manual restore test to a staging environment (see Section 8)

---

## 4. Recovery Procedures

### 4.1 Scenario: Complete Database Failure

**Symptoms:** PostgreSQL container fails to start, data volume corrupted, `pg_isready` fails.

**Estimated Recovery Time:** 30-60 minutes

**Procedure:**

```
Step 1: Assess the failure
   $ docker logs georgia-erp-db --tail 100
   $ docker inspect georgia-erp-db --format='{{.State.Status}}'
   $ docker volume inspect georgia-erp_postgres_data

Step 2: Attempt container restart
   $ docker compose restart postgres
   $ docker compose exec postgres pg_isready -U erp_user -d georgia_erp
   If successful -> verify data integrity (Step 6) -> DONE

Step 3: If restart fails, stop all dependent services
   $ docker compose stop api workers
   $ docker compose stop postgres

Step 4: Identify the most recent valid backup
   $ ./scripts/backup/pg_restore.sh --list-backups
   Select the most recent backup with a valid checksum.

Step 5: Restore from backup
   $ docker compose up -d postgres    # Start fresh PostgreSQL
   $ sleep 10                          # Wait for initialization

   # Restore using the backup script:
   $ PGPASSWORD=<password> ./scripts/backup/pg_restore.sh \
       /var/backups/georgia-erp/postgres/daily/georgia_erp_daily_YYYYMMDD_HHMMSS.dump

Step 6: Validate restored data
   $ docker compose exec postgres psql -U erp_user -d georgia_erp \
       -c "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';"
   $ docker compose exec postgres psql -U erp_user -d georgia_erp \
       -c "SELECT count(*) FROM audit_logs;"
   $ docker compose exec postgres psql -U erp_user -d georgia_erp \
       -c "SELECT max(created_at) FROM sales_orders;"
   Compare the latest record timestamp against the backup timestamp
   to understand data loss window.

Step 7: Restart application services
   $ docker compose up -d api workers

Step 8: Verify application health
   $ curl -f http://localhost:5000/health
   $ docker compose logs --tail 50 api
   $ docker compose logs --tail 50 workers

Step 9: Check RS.GE queue processing
   $ curl -u erp_user:<password> http://localhost:15672/api/queues
   Verify no unprocessed messages are stuck.

Step 10: Document the incident
   Record: failure time, detection time, recovery start, recovery end,
   data loss window, root cause (if known).
```

### 4.2 Scenario: Partial Data Corruption

**Symptoms:** Application errors on specific records, integrity constraint violations, inconsistent data between related tables.

**Estimated Recovery Time:** 1-2 hours (depends on corruption scope)

**Procedure:**

```
Step 1: Identify corrupted data
   $ docker compose exec postgres psql -U erp_user -d georgia_erp
   -- Check for orphaned records:
   SELECT * FROM sales_order_items WHERE order_id NOT IN (SELECT id FROM sales_orders);
   -- Check for constraint violations:
   SELECT conname, conrelid::regclass FROM pg_constraint WHERE NOT convalidated;

Step 2: Assess corruption scope
   Determine which tables and date ranges are affected.
   If corruption is limited to non-financial data -> consider manual repair.
   If financial/tax records are affected -> full restore required.

Step 3: Create a backup of the current (corrupted) state
   $ PGPASSWORD=<password> pg_dump -h localhost -U erp_user \
       -d georgia_erp -Fc -f /tmp/georgia_erp_corrupted_$(date +%Y%m%d).dump

Step 4a: Targeted table restore (if corruption is limited)
   # Restore specific tables from a known-good backup:
   $ pg_restore --host=localhost --username=erp_user \
       --dbname=georgia_erp \
       --table=<corrupted_table> \
       --clean --if-exists \
       /var/backups/georgia-erp/postgres/daily/<backup_file>.dump

Step 4b: Full restore (if corruption is widespread)
   Follow the Complete Database Failure procedure (Section 4.1).

Step 5: Validate data integrity
   -- Run referential integrity checks:
   SELECT 'invoices' AS tbl, count(*) FROM invoices
     WHERE customer_id NOT IN (SELECT id FROM customers);
   SELECT 'sales_order_items' AS tbl, count(*) FROM sales_order_items
     WHERE product_id NOT IN (SELECT id FROM products);

   -- Verify financial totals:
   SELECT date_trunc('day', created_at) AS day, sum(total_amount)
     FROM sales_orders
     WHERE created_at > now() - interval '7 days'
     GROUP BY 1 ORDER BY 1;

Step 6: Reconcile RS.GE records
   Compare local invoice records against RS.GE submitted records
   for the affected time period.

Step 7: Document findings
   Record: tables affected, records affected, data loss (if any),
   root cause analysis.
```

### 4.3 Scenario: RabbitMQ Failure

**Symptoms:** Message queue unavailable, RS.GE submissions failing, worker errors.

**Estimated Recovery Time:** 15-30 minutes

**Procedure:**

```
Step 1: Check RabbitMQ status
   $ docker logs georgia-erp-mq --tail 100
   $ docker compose exec rabbitmq rabbitmq-diagnostics check_running

Step 2: Attempt restart
   $ docker compose restart rabbitmq
   $ docker compose exec rabbitmq rabbitmq-diagnostics ping
   If successful -> verify queues (Step 5) -> DONE

Step 3: If restart fails, recreate the container
   $ docker compose stop rabbitmq
   $ docker compose rm -f rabbitmq
   $ docker compose up -d rabbitmq
   Wait for health check to pass.

Step 4: Restore definitions
   $ RABBITMQ_PASS=<password> ./scripts/backup/rabbitmq_backup.sh  # Find latest
   # Or restore directly:
   $ gunzip -k /var/backups/georgia-erp/rabbitmq/rabbitmq_definitions_YYYYMMDD_HHMMSS.json.gz
   $ curl -u erp_user:<password> -X POST \
       -H "Content-Type: application/json" \
       -d @/var/backups/georgia-erp/rabbitmq/rabbitmq_definitions_YYYYMMDD_HHMMSS.json \
       http://localhost:15672/api/definitions

Step 5: Verify queues and exchanges
   $ curl -s -u erp_user:<password> http://localhost:15672/api/queues | python3 -m json.tool
   Confirm all expected queues exist:
   - rsge.invoices
   - rsge.waybills
   - rsge.retry
   - rsge.dead-letter

Step 6: Restart dependent services
   $ docker compose restart api workers

Step 7: Monitor RS.GE submission recovery
   Watch worker logs for successful RS.GE submissions:
   $ docker compose logs -f workers --tail 20

Note: Messages that were in-flight during the failure are lost.
Check the PostgreSQL audit trail for any RS.GE submissions that
were queued but not confirmed, and re-submit them.
```

### 4.4 Scenario: Application Server Failure

**Symptoms:** API returns 5xx errors, containers crash-looping, deployment failure.

**Estimated Recovery Time:** 5-15 minutes

**Procedure:**

```
Step 1: Identify failing service
   $ docker compose ps
   $ docker compose logs --tail 50 api
   $ docker compose logs --tail 50 workers

Step 2: Attempt restart
   $ docker compose restart api workers
   $ curl -f http://localhost:5000/health
   If successful -> DONE

Step 3: If code issue, roll back to previous image
   $ docker compose pull   # If using registry
   # Or rebuild from last known good commit:
   $ git log --oneline -5
   $ git checkout <last-good-commit>
   $ docker compose build api workers
   $ docker compose up -d api workers

Step 4: Verify database connectivity
   $ docker compose exec api dotnet --info   # Verify runtime
   $ curl -f http://localhost:5000/health

Step 5: Verify external integrations
   - RS.GE SOAP endpoint reachable
   - RabbitMQ connected
   - Redis connected (if applicable)
```

### 4.5 Scenario: Complete Site Failure

**Symptoms:** All services down, infrastructure unrecoverable (hardware failure, hosting provider outage, data center loss).

**Estimated Recovery Time:** 2-4 hours

**Prerequisites:**
- Access to off-site backups (S3 or other remote storage)
- Docker and Docker Compose installed on new host
- Git access to source repository
- Network/DNS control

**Procedure:**

```
Step 1: Provision new infrastructure
   - Deploy a server meeting minimum requirements:
     - 4+ CPU cores, 8+ GB RAM, 100+ GB SSD
     - Docker Engine 24+ and Docker Compose v2
     - PostgreSQL 16 client tools
   - Allocate static IP (required for RS.GE IP whitelisting)

Step 2: Clone application repository
   $ git clone <repository-url>
   $ cd Enterprise-Retail-ERP-Platform-

Step 3: Configure environment
   - Copy and edit environment configuration
   - Update connection strings, secrets, RS.GE credentials
   - Update the RS.GE service user IP whitelist if IP changed:
     Call update_service_user with the new server IP

Step 4: Start infrastructure services
   $ docker compose up -d postgres rabbitmq
   Wait for health checks to pass.

Step 5: Restore database from off-site backup
   # Download latest backup from S3:
   $ aws s3 cp s3://<bucket>/backups/postgres/daily/<latest>.dump /tmp/restore.dump
   # Or from weekly/monthly if daily is unavailable

   $ PGPASSWORD=<password> ./scripts/backup/pg_restore.sh /tmp/restore.dump --no-confirm

Step 6: Restore RabbitMQ definitions
   $ aws s3 cp s3://<bucket>/backups/rabbitmq/<latest>.json.gz /tmp/rmq_defs.json.gz
   $ gunzip /tmp/rmq_defs.json.gz
   $ curl -u erp_user:<password> -X POST \
       -H "Content-Type: application/json" \
       -d @/tmp/rmq_defs.json http://localhost:15672/api/definitions

Step 7: Build and start application services
   $ docker compose build api workers
   $ docker compose up -d api workers

Step 8: Update DNS records
   Point domain names to the new server IP.
   Allow TTL to expire (or flush DNS caches).

Step 9: Full validation (see Section 6)

Step 10: Re-enable cron backups on new server
   $ sudo ./scripts/backup/backup-cron.sh install
```

---

## 5. Communication Plan

### 5.1 Escalation Matrix

| Severity | Response Time | Notification | Approval Needed |
|----------|-------------|-------------|----------------|
| SEV-1: Complete outage | Immediate | All stakeholders, management | None (act first) |
| SEV-2: Degraded service | 15 minutes | Engineering team, operations | Team lead |
| SEV-3: Single component | 30 minutes | Engineering team | None |
| SEV-4: Non-critical | Next business day | Ticket system | None |

### 5.2 Notification Templates

**Initial Incident Notification:**
```
Subject: [INCIDENT] Georgia ERP - <Severity> - <Brief Description>

Status: INVESTIGATING
Impact: <What is affected>
Started: <Time in Tbilisi timezone, UTC+4>
ETA: <Estimated resolution or "assessing">

We are aware of an issue affecting <component>. The team is
actively investigating. Next update in <30 minutes>.
```

**Resolution Notification:**
```
Subject: [RESOLVED] Georgia ERP - <Brief Description>

Status: RESOLVED
Impact: <What was affected>
Duration: <Start time> to <End time> (<total duration>)
Data Loss: <None / describe affected records>
Root Cause: <Brief explanation>

Full post-incident review will be completed within 48 hours.
```

### 5.3 RS.GE Compliance Notification

If an outage affects RS.GE submissions, the following additional steps are required:

1. Document all RS.GE submissions that failed or were delayed during the outage
2. Re-submit any pending invoices/waybills after recovery
3. If submission deadlines were missed, contact RS.GE support with incident documentation
4. Retain all evidence of the outage for audit purposes

---

## 6. Post-Recovery Validation Checklist

After any recovery, complete ALL items before declaring the incident resolved:

### 6.1 Infrastructure Validation

- [ ] PostgreSQL accepting connections: `pg_isready -h localhost -U erp_user`
- [ ] RabbitMQ management UI accessible: `http://<host>:15672`
- [ ] Redis responding (if applicable): `redis-cli ping`
- [ ] All Docker containers healthy: `docker compose ps`
- [ ] No container restart loops: `docker compose ps` (check restart count)

### 6.2 Application Validation

- [ ] API health endpoint returns 200: `curl -f http://localhost:5000/health`
- [ ] User authentication working: test login with a known account
- [ ] API responds to basic queries: list products, list customers
- [ ] Worker service processing jobs: check worker logs

### 6.3 Data Integrity Validation

- [ ] All expected database tables exist
- [ ] Record counts are within expected range (compare with pre-incident metrics)
- [ ] Financial totals reconcile with known checkpoints
- [ ] Audit trail is intact: `SELECT count(*), min(created_at), max(created_at) FROM audit_logs`
- [ ] No orphaned records in foreign key relationships
- [ ] Sequence values are correct (no ID conflicts)

### 6.4 RS.GE Compliance Validation

- [ ] RS.GE SOAP endpoint reachable from the server
- [ ] Service user credentials valid: call `chek_service_user`
- [ ] IP whitelist current (especially after infrastructure changes)
- [ ] Pending invoice queue is processing
- [ ] Pending waybill queue is processing
- [ ] Compare local invoice count vs. RS.GE submitted count for the recovery period
- [ ] No duplicate RS.GE submissions created during recovery

### 6.5 Monitoring Validation

- [ ] Backup cron jobs are installed and scheduled
- [ ] Log rotation is configured
- [ ] Alert/notification channels are functional (test email or webhook)
- [ ] Monitoring dashboards show recovered metrics

---

## 7. Enhanced Recovery Options

### 7.1 WAL Archiving for Continuous Backup (Achieving 1-Hour RPO)

For production environments requiring RPO below 24 hours, enable PostgreSQL WAL (Write-Ahead Log) archiving:

**PostgreSQL Configuration (`postgresql.conf`):**
```
wal_level = replica
archive_mode = on
archive_command = 'test ! -f /var/backups/wal/%f && cp %p /var/backups/wal/%f'
archive_timeout = 300    # Archive every 5 minutes maximum
```

**Docker Compose Override (`docker-compose.prod.yml`):**
```yaml
services:
  postgres:
    volumes:
      - postgres_data:/var/lib/postgresql/data
      - wal_archive:/var/backups/wal
    command: >
      postgres
        -c wal_level=replica
        -c archive_mode=on
        -c archive_command='test ! -f /var/backups/wal/%f && cp %p /var/backups/wal/%f'
        -c archive_timeout=300
```

**Point-in-Time Recovery (PITR) Procedure:**
```
1. Stop PostgreSQL
2. Restore base backup (from pg_basebackup or volume backup)
3. Create recovery.signal file
4. Configure restore_command in postgresql.conf:
   restore_command = 'cp /var/backups/wal/%f %p'
   recovery_target_time = '2026-06-20 14:30:00+04'
5. Start PostgreSQL -- it will replay WAL up to the target time
```

### 7.2 Streaming Replication (Hot Standby)

For production environments requiring RTO below 15 minutes:

```yaml
# docker-compose.prod.yml - Add standby server
services:
  postgres-standby:
    image: postgres:16-alpine
    environment:
      PGUSER: replicator
      PGPASSWORD: replication_password
    command: >
      bash -c "
        pg_basebackup -h postgres -U replicator -D /var/lib/postgresql/data -Fp -Xs -P -R
        && postgres
      "
    depends_on:
      - postgres
```

---

## 8. DR Drill Schedule

Regular DR drills validate that procedures work and the team is prepared.

### 8.1 Drill Types

| Drill Type | Frequency | Duration | Description |
|-----------|-----------|----------|-------------|
| Backup Restore Test | Monthly | 1 hour | Restore latest backup to staging, validate data |
| Component Failover | Quarterly | 2 hours | Simulate single component failure, execute recovery |
| Full DR Simulation | Semi-annually | 4 hours | Simulate complete site failure on a test environment |
| Tabletop Exercise | Annually | 2 hours | Walk through scenarios with the full team, no live systems |

### 8.2 Monthly Backup Restore Test Procedure

```
1. Select the latest daily backup
2. Provision a temporary PostgreSQL instance (or use staging)
3. Restore the backup using pg_restore.sh
4. Run the validation checklist (Section 6.3)
5. Record results: restore time, data completeness, any issues
6. Destroy the temporary instance
7. File the drill report
```

### 8.3 Drill Report Template

```
DR Drill Report
Date:        ____________________
Drill Type:  ____________________
Participants: ____________________

Scenario Tested: ____________________

Timeline:
  Drill started:     ____________________
  Recovery started:  ____________________
  Recovery complete: ____________________
  Validation done:   ____________________

Results:
  RTO achieved:      ____ minutes (target: ____ minutes)
  RPO achieved:      ____ (data loss window)
  All validations passed: Yes / No

Issues Found:
  1. ____________________
  2. ____________________

Action Items:
  1. ____________________
  2. ____________________

Sign-off: ____________________
```

---

## 9. Document History

| Version | Date | Author | Changes |
|---------|------|--------|---------|
| 1.0 | June 2026 | Platform Team | Initial DR plan |

---

## 10. Related Documents

- [02 - Solution Architecture](02-SOLUTION-ARCHITECTURE.md)
- [03 - Database Design](03-DATABASE-DESIGN.md)
- [04 - RS.GE Technical Analysis](04-RSGE-TECHNICAL-ANALYSIS.md)
- [06 - Security Architecture](06-SECURITY-ARCHITECTURE.md)
- [12 - Data Retention Policy](12-DATA-RETENTION-POLICY.md)
