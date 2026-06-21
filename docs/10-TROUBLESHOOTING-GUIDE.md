# Troubleshooting Guide

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Last Updated:** June 2026

---

## Table of Contents

1. [Quick Diagnostic Checklist](#1-quick-diagnostic-checklist)
2. [Database Issues](#2-database-issues)
3. [RabbitMQ Issues](#3-rabbitmq-issues)
4. [RS.GE SOAP Integration Issues](#4-rsge-soap-integration-issues)
5. [Authentication and Authorization Issues](#5-authentication-and-authorization-issues)
6. [API Issues](#6-api-issues)
7. [Docker Container Issues](#7-docker-container-issues)
8. [Desktop Client Issues](#8-desktop-client-issues)
9. [Performance Issues](#9-performance-issues)
10. [Log File Reference](#10-log-file-reference)

---

## 1. Quick Diagnostic Checklist

When something is not working, start here:

| Check | Command | Expected |
|-------|---------|----------|
| API health | `curl http://localhost:5000/health` | `Healthy` |
| RS.GE connectivity | `curl http://localhost:5000/api/v1/compliance/rsge/health` | `Status: Connected` |
| Database up | `docker exec georgia-erp-db pg_isready -U erp_user` | `accepting connections` |
| RabbitMQ up | `docker exec georgia-erp-mq rabbitmq-diagnostics -q ping` | `Ping succeeded` |
| Container status | `docker compose ps` | All services `Up (healthy)` |
| API logs | `docker logs --tail 50 georgia-erp-api` | No ERROR entries |
| Worker logs | `docker logs --tail 50 georgia-erp-workers` | No ERROR entries |

---

## 2. Database Issues

### Problem: API returns "Unhealthy" at /health

**Symptoms:**
- `GET /health` returns `Unhealthy`
- API logs show `Npgsql.NpgsqlException` or `connection refused`

**Diagnosis:**

```bash
# Check if PostgreSQL container is running
docker compose ps postgres

# Check PostgreSQL logs
docker logs georgia-erp-db

# Test connectivity from API container
docker exec georgia-erp-api ping postgres

# Test direct connection
docker exec georgia-erp-db psql -U erp_user -d georgia_erp -c "SELECT 1;"
```

**Solutions:**

1. **Container not running:** `docker compose up -d postgres`
2. **Wrong password:** Check `POSTGRES_PASSWORD` matches `ConnectionStrings__DefaultConnection`
3. **Database not created:** The database is auto-created on first `docker compose up`. If lost, recreate:
   ```bash
   docker compose down -v
   docker compose up -d
   ```
4. **Disk full:** Check host disk space. PostgreSQL stops accepting writes when the data volume is full.
   ```bash
   docker system df
   df -h /var/lib/docker
   ```

---

### Problem: Migration Errors on Startup

**Symptoms:**
- API fails to start with migration-related errors
- Log shows `Microsoft.EntityFrameworkCore.Database.Migration` errors

**Diagnosis:**

```bash
# Check current migration status
cd src/GeorgiaERP.Api
dotnet ef migrations list --connection "Host=localhost;Database=georgia_erp;Username=erp_user;Password=erp_dev_password"
```

**Solutions:**

1. **Pending migrations:** Run manually:
   ```bash
   dotnet ef database update --connection "Host=localhost;..."
   ```

2. **Conflicting migrations:** If migrations are out of order (common after branch merges):
   ```bash
   # Generate idempotent script and review
   dotnet ef migrations script --idempotent -o fix.sql
   # Apply manually
   docker exec -i georgia-erp-db psql -U erp_user -d georgia_erp < fix.sql
   ```

3. **Schema mismatch:** If the database schema is ahead of the code (e.g., after a rollback), check which migration was last applied:
   ```sql
   SELECT * FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 5;
   ```

---

### Problem: Slow Database Queries

**Symptoms:**
- API responses are slow (> 1 second)
- Logs show long query execution times

**Diagnosis:**

```sql
-- Connect to the database
docker exec -it georgia-erp-db psql -U erp_user -d georgia_erp

-- Check active queries
SELECT pid, now() - pg_stat_activity.query_start AS duration, query, state
FROM pg_stat_activity
WHERE state != 'idle'
ORDER BY duration DESC;

-- Check table sizes
SELECT relname, pg_size_pretty(pg_total_relation_size(relid))
FROM pg_catalog.pg_statio_user_tables
ORDER BY pg_total_relation_size(relid) DESC;

-- Check missing indexes
SELECT schemaname, tablename, seq_scan, seq_tup_read,
       idx_scan, idx_tup_fetch
FROM pg_stat_user_tables
WHERE seq_scan > idx_scan
ORDER BY seq_tup_read DESC
LIMIT 20;
```

**Solutions:**

1. Run `ANALYZE` on heavily-used tables to update statistics
2. Add indexes for frequently-queried columns (especially foreign keys)
3. Review EF Core queries in handlers -- add `.AsNoTracking()` for read-only queries

---

## 3. RabbitMQ Issues

### Problem: RabbitMQ Container Fails to Start

**Symptoms:**
- RabbitMQ container exits immediately or keeps restarting
- Workers cannot connect to RabbitMQ

**Diagnosis:**

```bash
# Check container status and logs
docker compose ps rabbitmq
docker logs georgia-erp-mq
```

**Solutions:**

1. **Port conflict:** Another service is using port 5672 or 15672:
   ```bash
   # Windows
   netstat -ano | findstr :5672
   # Linux
   ss -tlnp | grep 5672
   ```
   Stop the conflicting service or change the port mapping.

2. **Corrupted data volume:** Clear and restart:
   ```bash
   docker compose down
   docker volume rm enterprise-retail-erp-platform-_rabbitmq_data
   docker compose up -d
   ```

3. **Memory limit:** RabbitMQ stops accepting connections when memory exceeds its threshold (default: 40% of system RAM). Check with:
   ```bash
   docker exec georgia-erp-mq rabbitmq-diagnostics memory_breakdown
   ```

---

### Problem: Messages Stuck in Queue

**Symptoms:**
- Waybills remain in `Queued` status
- No RS.GE submissions happening
- RabbitMQ management UI shows messages accumulating

**Diagnosis:**

```bash
# Check worker is running
docker compose ps workers

# Check worker logs for errors
docker logs --tail 100 georgia-erp-workers

# Check queue status via management API
curl -u erp_user:erp_dev_password http://localhost:15672/api/queues
```

**Solutions:**

1. **Worker not running:** `docker compose up -d workers`
2. **Worker crashed:** Check logs for unhandled exceptions, then restart:
   ```bash
   docker compose restart workers
   ```
3. **Connection lost:** Worker lost connection to RabbitMQ. Restart the worker.
4. **Dead letter queue:** Messages that fail repeatedly are moved to the dead letter queue. Inspect and requeue:
   ```bash
   # Via RabbitMQ management UI at http://localhost:15672
   # Navigate to Queues -> dead letter queue -> Get Messages
   ```

---

### Problem: Worker Cannot Connect to RabbitMQ

**Symptoms:**
- Worker logs show `RabbitMQ.Client.Exceptions.BrokerUnreachableException`

**Diagnosis:**

Check that the RabbitMQ hostname, username, and password in the worker configuration match the RabbitMQ container:

```bash
# Check worker env vars
docker inspect georgia-erp-workers | grep -A 20 "Env"
```

**Solutions:**

1. Verify `RsGe__Queue__HostName` is set to the correct hostname (`rabbitmq` in Docker Compose)
2. Verify credentials match `RABBITMQ_DEFAULT_USER` and `RABBITMQ_DEFAULT_PASS`
3. Wait for RabbitMQ health check to pass before starting workers (Docker Compose `depends_on` handles this)

---

## 4. RS.GE SOAP Integration Issues

### Problem: RS.GE Health Check Returns "Unavailable"

**Symptoms:**
- `GET /api/v1/compliance/rsge/health` returns `Status: Unavailable`
- Waybill submissions fail

**Diagnosis:**

```bash
# Check if RS.GE is reachable from the API container
docker exec georgia-erp-api curl -s https://waybill.rs.ge/

# Check DNS resolution
docker exec georgia-erp-api nslookup waybill.rs.ge

# Check worker logs for SOAP errors
docker logs --tail 50 georgia-erp-workers | grep -i "rsge\|soap\|waybill"
```

**Common Causes:**

1. **RS.GE is down:** The Georgian Revenue Service occasionally has maintenance windows. Check `https://rs.ge` directly.
2. **Network/firewall:** The API container cannot reach the internet. Ensure Docker has DNS and outbound access.
3. **SSL/TLS issues:** Certificate verification failures. Check system certificates.

---

### Problem: Waybill Submission Fails

**Symptoms:**
- Fiscal document status is `Failed`
- `lastError` field contains SOAP error details
- `retryCount` is incrementing

**Diagnosis:**

```sql
-- Check failed fiscal documents
SELECT id, document_type, internal_ref, status, retry_count, last_error, created_at
FROM fiscal_documents
WHERE status = 'Failed'
ORDER BY created_at DESC
LIMIT 20;

-- Check specific waybill details
SELECT w.*, fd.last_error
FROM rsge_waybills w
JOIN fiscal_documents fd ON fd.id = w.fiscal_document_id
WHERE fd.status = 'Failed'
ORDER BY w.created_at DESC
LIMIT 10;
```

**Common RS.GE SOAP Errors:**

| Error Message | Cause | Solution |
|--------------|-------|----------|
| `Invalid TIN` | Buyer or seller TIN is incorrect | Verify TIN format (9-11 digits) |
| `Unauthorized` | RS.GE credentials expired | Update RS.GE SOAP credentials in config |
| `Duplicate waybill` | Waybill was already submitted | Check if a previous submission succeeded |
| `Invalid unit` | Unit ID not recognized by RS.GE | Use `GET /api/v1/compliance/rsge/units` to get valid unit IDs |
| `Transport type required` | Missing transport information | Provide vehicleNumber, driverTin, and transportType |
| `Connection timeout` | RS.GE server is slow or down | Wait and retry; the worker retries automatically |
| `Server Error (500)` | RS.GE internal error | Contact RS.GE support if persistent |

**Manual Retry:**

The worker automatically retries failed submissions. To force a retry, update the fiscal document status:

```sql
UPDATE fiscal_documents
SET status = 'Queued', retry_count = 0, last_error = NULL
WHERE id = '<document-id>';
```

Then enqueue the operation again via the API:

```bash
curl -X POST http://localhost:5000/api/v1/compliance/waybills/<fiscal-document-id>/confirm \
  -H "Authorization: Bearer <token>"
```

---

### Problem: Waybill Data Mismatch

**Symptoms:**
- Waybill submitted but RS.GE shows different data
- Goods items missing or incorrect

**Diagnosis:**

The waybill goods data is stored as JSON in the `goods_data` column of `rsge_waybills`:

```sql
SELECT id, goods_data, total_amount, buyer_tin, seller_tin
FROM rsge_waybills
WHERE id = '<waybill-id>';
```

Check that the JSON matches the expected goods items (product names, quantities, prices, unit IDs).

---

### Problem: Compliance Deadlines Approaching

**Symptoms:**
- `GET /api/v1/compliance/deadlines` shows overdue documents

**Response:**

1. Check overdue documents and their last errors
2. Fix any configuration issues (TINs, credentials, etc.)
3. Requeue failed documents for submission
4. Monitor the worker logs to confirm resubmission

---

## 5. Authentication and Authorization Issues

### Problem: Login Returns 401 Unauthorized

**Symptoms:**
- `POST /api/v1/auth/login` returns 401
- Error message: "Invalid username or password"

**Diagnosis:**

```sql
-- Check if user exists and is active
SELECT id, username, is_active, is_locked_out, failed_login_count
FROM users
WHERE username = '<username>';
```

**Solutions:**

1. **Wrong credentials:** Verify username and password
2. **Account locked:** Too many failed attempts. Reset:
   ```sql
   UPDATE users SET is_locked_out = false, failed_login_count = 0
   WHERE username = '<username>';
   ```
3. **Account inactive:** Reactivate:
   ```sql
   UPDATE users SET is_active = true WHERE username = '<username>';
   ```
4. **2FA required:** If 2FA is enabled, include the `twoFactorCode` in the login request

---

### Problem: 401 on Authenticated Endpoints

**Symptoms:**
- Endpoints return 401 despite providing a token
- Token was working previously

**Diagnosis:**

1. **Token expired:** JWT tokens have a limited lifetime. Check the `expiresAt` field from the login response.
2. **Refresh the token:** Use `POST /api/v1/auth/refresh` with the refresh token.
3. **JWT secret changed:** If the `Jwt__SecretKey` environment variable changed, all existing tokens are invalid. Users must re-login.

**Debugging token issues:**

Decode the JWT at `https://jwt.io` to inspect:
- `exp` (expiration timestamp)
- `iss` (issuer -- must match `Jwt__Issuer`)
- `aud` (audience -- must match `Jwt__Audience`)

---

### Problem: 429 Too Many Requests

**Symptoms:**
- Login or API calls return HTTP 429

**Cause:** Rate limiting is active:
- Auth endpoints: 10 requests per minute per IP
- General endpoints: 100 requests per minute per IP

**Solutions:**
- Wait for the rate limit window to reset (1 minute)
- If behind a load balancer, ensure `X-Forwarded-For` is set so rate limiting uses the real client IP, not the proxy IP

---

## 6. API Issues

### Problem: API Fails to Start

**Symptoms:**
- Container exits immediately
- `docker logs georgia-erp-api` shows startup errors

**Common Causes:**

1. **Missing environment variables:** Check all required env vars are set (see Deployment Guide section 3)
2. **Database not ready:** API depends on PostgreSQL being healthy. Check the health check.
3. **Port conflict:** Another process is using port 5000:
   ```bash
   netstat -ano | findstr :5000
   ```
4. **Migration failure:** Check for EF Core migration errors in logs

---

### Problem: API Returns 500 Internal Server Error

**Symptoms:**
- Endpoints return 500 with generic error message
- Detailed error in logs only

**Diagnosis:**

```bash
# Check API logs for the exception
docker logs --tail 200 georgia-erp-api | grep -A 10 "ERR\|Exception\|500"
```

The `ExceptionHandlingMiddleware` catches unhandled exceptions and returns a generic error to clients (OWASP best practice). The full stack trace is in the logs.

**Common Causes:**

1. **Null reference:** Check for missing navigation properties in EF Core queries
2. **Database constraint violation:** Unique constraint or foreign key violation
3. **Timeout:** Database query or RS.GE SOAP call timed out

---

### Problem: CORS Errors in Browser

**Symptoms:**
- Browser console shows `Access-Control-Allow-Origin` errors
- API requests from frontend fail

**Solutions:**

1. Check `Cors__Origins` configuration matches the frontend URL exactly (including protocol and port)
2. Ensure the API `Program.cs` has `app.UseCors("AllowFrontend")` before `app.UseAuthorization()`
3. Default allowed origin is `http://localhost:3000`. For production, set:
   ```
   Cors__Origins__0=https://erp.yourcompany.ge
   ```

---

### Problem: Request Body Too Large

**Symptoms:**
- API returns 413 (Payload Too Large) or request is rejected

**Cause:** The `RequestSizeLimitMiddleware` enforces maximum request body size.

**Solution:** For legitimate large payloads (bulk imports), adjust the size limit in the middleware configuration or use paginated/chunked uploads.

---

## 7. Docker Container Issues

### Problem: Containers Keep Restarting

**Diagnosis:**

```bash
# Check restart count and status
docker compose ps

# Check container exit code
docker inspect georgia-erp-api --format='{{.State.ExitCode}}'

# Check logs for the crash reason
docker logs --tail 100 georgia-erp-api
```

**Common Exit Codes:**

| Exit Code | Meaning | Action |
|-----------|---------|--------|
| 0         | Normal exit | Check why it stopped (manual stop?) |
| 1         | Application error | Check logs for exception |
| 137       | OOM killed | Increase container memory limit |
| 139       | Segfault | Report as bug |
| 143       | SIGTERM received | Normal shutdown signal |

---

### Problem: Container Cannot Resolve Other Service Names

**Symptoms:**
- API cannot connect to `postgres` or `rabbitmq` hostnames
- Errors like `Name or service not known`

**Solutions:**

1. Ensure all services are in the same Docker Compose file
2. Use service names (not `localhost`) for inter-container communication
3. Check Docker network:
   ```bash
   docker network ls
   docker network inspect enterprise-retail-erp-platform-_default
   ```

---

### Problem: Out of Disk Space

**Symptoms:**
- Containers fail to start
- Database errors about write failures

**Diagnosis:**

```bash
# Check Docker disk usage
docker system df

# Check host disk space
df -h

# List large Docker items
docker system df -v
```

**Solutions:**

1. Remove unused Docker images, containers, and volumes:
   ```bash
   docker system prune -a --volumes
   ```
2. Rotate/compress log files
3. Clean old database backups

---

### Problem: Rebuilding After Code Changes

If containers are running old code after a `git pull`:

```bash
# Rebuild and restart
docker compose up -d --build

# Force full rebuild (no cache)
docker compose build --no-cache
docker compose up -d
```

---

## 8. Desktop Client Issues

### Problem: Desktop Client Cannot Connect to API

**Symptoms:**
- Login screen shows connection error
- Desktop app shows "Server Unavailable"

**Diagnosis:**

1. Check API is running: `curl http://localhost:5000/health`
2. Check the Desktop client API URL configuration
3. Check Windows firewall is not blocking the connection

---

### Problem: Desktop Client Crashes on Startup

**Diagnosis:**

Check the application event log or crash dump. Common causes:

1. **.NET runtime not installed:** Ensure .NET 9 Desktop Runtime is installed
2. **Missing DLLs:** Rebuild the Desktop project:
   ```bash
   dotnet build src/GeorgiaERP.Desktop/GeorgiaERP.Desktop.csproj
   ```
3. **Configuration file missing:** Check `appsettings.json` exists in the output directory

---

## 9. Performance Issues

### Problem: Slow API Response Times

**Diagnosis:**

1. **Check Serilog request logs:** Each request is logged with duration:
   ```
   HTTP GET /api/v1/products responded 200 in 145ms
   ```

2. **Enable detailed EF Core logging** for slow queries:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Microsoft.EntityFrameworkCore.Database.Command": "Information"
       }
     }
   }
   ```

3. **Check database connection pool:** Monitor active connections:
   ```sql
   SELECT count(*) FROM pg_stat_activity WHERE datname = 'georgia_erp';
   ```

**Solutions:**

1. Add `.AsNoTracking()` to read-only queries
2. Add database indexes for frequently-filtered columns
3. Review N+1 query patterns -- use `.Include()` for related data
4. Increase PostgreSQL `max_connections` if pool is exhausted
5. Response compression (Brotli/Gzip) is enabled by default for JSON responses

---

### Problem: Memory Usage Growing Over Time

**Diagnosis:**

```bash
# Check container memory usage
docker stats georgia-erp-api georgia-erp-workers
```

**Solutions:**

1. Set memory limits in Docker Compose:
   ```yaml
   deploy:
     resources:
       limits:
         memory: 2G
   ```
2. Check for memory leaks in long-running handlers
3. Restart containers periodically if necessary (use `restart: unless-stopped`)

---

## 10. Log File Reference

### Log Locations

| Service | Location | Format |
|---------|----------|--------|
| API (file) | `logs/georgia-erp-YYYYMMDD.log` | Serilog structured text |
| API (container) | `docker logs georgia-erp-api` | Console output |
| Workers (container) | `docker logs georgia-erp-workers` | Console output |
| PostgreSQL | `docker logs georgia-erp-db` | PostgreSQL native format |
| RabbitMQ | `docker logs georgia-erp-mq` | RabbitMQ native format |

### Log Format

```
{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}
```

Example:

```
2026-06-20 12:00:00.123 +04:00 [INF] Georgia ERP Platform starting up...
2026-06-20 12:00:01.456 +04:00 [INF] HTTP GET /health responded 200 in 12ms
2026-06-20 12:00:05.789 +04:00 [ERR] Unhandled exception processing request
System.InvalidOperationException: Sequence contains no elements
   at System.Linq.ThrowHelper.ThrowNoElementsException()
   at ...
```

### Log Levels

| Level | Abbreviation | Used For |
|-------|-------------|----------|
| Verbose | VRB | Detailed diagnostic events (off by default) |
| Debug | DBG | Internal events useful for debugging |
| Information | INF | Normal operations (startup, requests, business events) |
| Warning | WRN | Unexpected but handled situations |
| Error | ERR | Failures that need attention |
| Fatal | FTL | Application cannot continue |

### File Retention

Log files rotate daily and are retained for 14 days. The naming pattern is:

```
logs/georgia-erp-20260620.log
logs/georgia-erp-20260619.log
...
```

### Useful Log Search Commands

```bash
# Find all errors in today's API logs
docker logs georgia-erp-api 2>&1 | grep "\[ERR\]"

# Find RS.GE related log entries
docker logs georgia-erp-workers 2>&1 | grep -i "rsge\|waybill\|soap"

# Find slow requests (> 1000ms)
docker logs georgia-erp-api 2>&1 | grep -E "responded [0-9]+ in [0-9]{4,}ms"

# Find authentication failures
docker logs georgia-erp-api 2>&1 | grep -i "unauthorized\|login\|401"

# Follow logs in real time
docker logs -f georgia-erp-api
```
