# Security Architecture Document

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 18, 2026
**Status:** Draft — Awaiting Review

---

## 1. Security Principles

1. **Defense in Depth** — multiple layers of security controls
2. **Least Privilege** — minimum access required for each role
3. **Zero Trust** — verify every request, trust nothing implicitly
4. **Audit Everything** — complete trail of all security-relevant actions
5. **Secure by Default** — secure configuration out of the box

---

## 2. Authentication

### 2.1 JWT Token Architecture

```
Login Request (username + password)
    │
    ├── Validate credentials (bcrypt hash comparison)
    ├── Check account status (active, not locked)
    ├── Check IP restrictions
    │
    ├── 2FA Required?
    │   ├── Yes → Return 2FA challenge
    │   │         └── Validate TOTP/SMS code
    │   └── No → Continue
    │
    ├── Generate Access Token (JWT)
    │   ├── Expiry: 15 minutes
    │   ├── Claims: user_id, company_id, roles[], store_ids[]
    │   ├── Algorithm: RS256 (RSA asymmetric)
    │   └── Issuer + Audience validation
    │
    ├── Generate Refresh Token
    │   ├── Opaque random token (not JWT)
    │   ├── Stored in database (hashed)
    │   ├── Expiry: 7 days
    │   ├── Bound to device fingerprint
    │   └── Single-use (rotated on refresh)
    │
    └── Return { access_token, refresh_token, expires_in }
```

### 2.2 Two-Factor Authentication

| Method | Implementation | Priority |
|--------|---------------|----------|
| TOTP (Authenticator App) | RFC 6238, Google Authenticator / Authy compatible | MVP |
| SMS | Georgian SMS provider integration | Phase 2 |

### 2.3 Session Management

- Access tokens: short-lived (15 min), stateless verification
- Refresh tokens: longer-lived (7 days), database-backed, rotatable
- Concurrent session limit: configurable per role (default: 3)
- Forced logout capability for administrators
- Session tracking with device info and IP

### 2.4 Password Policy

| Policy | Requirement |
|--------|------------|
| Minimum Length | 10 characters |
| Complexity | At least 1 uppercase, 1 lowercase, 1 digit, 1 special character |
| History | Cannot reuse last 5 passwords |
| Expiry | 90 days (configurable) |
| Lockout | 5 failed attempts → 15 minute lockout |
| Hashing | bcrypt with cost factor 12 |

---

## 3. Authorization (RBAC)

### 3.1 Permission Model

```
User
  └── assigned to Role(s)
       └── each Role has Permission(s)
            └── each Permission = Module + Action + Resource

Permission example:
  Module: INVENTORY
  Action: CREATE
  Resource: stock_adjustments
  
Scope:
  - Global: applies to all stores/warehouses
  - Store-specific: applies to assigned stores only
  - Own: applies to own records only
```

### 3.2 Built-in Role Definitions

```
SYSTEM_ADMIN
├── All permissions, all modules, global scope

COMPANY_ADMIN
├── All permissions except system administration
├── Global scope within company

STORE_MANAGER
├── POS: all actions
├── Inventory: read, create, update (own store)
├── Products: read
├── Pricing: read
├── Reports: read (own store)
├── Users: read (own store staff)

CASHIER
├── POS: create transactions, returns (with approval)
├── Products: read
├── Inventory: read (own store)

WAREHOUSE_OPERATOR
├── Inventory: all actions (assigned warehouse)
├── Warehouse: all actions (assigned warehouse)
├── Products: read
├── Procurement: read

PROCUREMENT_OFFICER
├── Procurement: all actions
├── Suppliers: all actions
├── Products: read, update (cost fields)
├── Inventory: read

ACCOUNTANT
├── Finance: all actions
├── Compliance: read
├── Reports: all
├── POS: read
├── Procurement: read

COMPLIANCE_OFFICER
├── Compliance: all actions
├── Finance: read (VAT-related)
├── Reports: read (compliance reports)

REGIONAL_MANAGER
├── Same as Store Manager but across multiple stores
├── Approval: approve actions from managed stores

EXECUTIVE
├── Reports: read (all)
├── Dashboards: read (all)
├── All modules: read-only
```

### 3.3 API-Level Authorization

```csharp
// Every API endpoint is decorated with required permission
[Authorize(Policy = "Inventory.Create.StockAdjustments")]
[HttpPost("adjustments")]
public async Task<IActionResult> CreateAdjustment(...)

// Middleware chain:
// 1. JWT validation (authentication)
// 2. Tenant resolution (company_id from token)
// 3. Permission check (role → permissions → endpoint)
// 4. Scope check (store_id/warehouse_id access)
// 5. Rate limiting
// 6. Audit logging
```

---

## 4. Data Security

### 4.1 Encryption

| Data State | Method | Implementation |
|-----------|--------|---------------|
| In Transit | TLS 1.3 | Nginx/YARP terminates TLS |
| At Rest (database) | PostgreSQL TDE or volume encryption | AWS RDS encryption / Azure encryption |
| At Rest (sensitive fields) | AES-256-GCM column-level encryption | Encrypt PII, bank details, passwords |
| At Rest (backups) | AES-256 | Encrypted before upload to storage |
| At Rest (file storage) | Server-side encryption | S3 SSE-S3 or Azure SSE |

### 4.2 Sensitive Data Handling

| Data Type | Storage | Access | Logging |
|-----------|---------|--------|---------|
| Passwords | bcrypt hash only | Never retrievable | Hash changes logged |
| TOTP Secrets | AES-256 encrypted | Auth service only | Access logged |
| Bank Account Numbers | AES-256 encrypted | Finance role only | Access logged |
| TIN (Tax ID) | Plaintext (public data) | All authenticated | — |
| Customer PII | AES-256 encrypted (phone, email) | CRM role + consent | Access logged |
| Payment Card Data | Never stored (PCI compliance) | Terminal handles | — |

### 4.3 Multi-Tenant Data Isolation

```
PostgreSQL Row-Level Security (RLS):

-- Enable RLS on all business tables
ALTER TABLE products ENABLE ROW LEVEL SECURITY;

-- Policy: users can only see data from their company's schema
-- Enforced at database level, not just application level

-- Additional isolation:
-- 1. Schema-per-tenant (physical separation)
-- 2. Connection string per tenant (separate credentials)
-- 3. Application-level tenant context (middleware)
-- 4. Query-level tenant filter (EF Core global query filters)
```

---

## 5. Network Security

### 5.1 Network Architecture

```
Internet
    │
    ▼
┌──────────────┐
│  CDN / WAF   │  (Cloudflare or AWS CloudFront)
│  DDoS prot.  │
└──────┬───────┘
       │
┌──────▼───────┐
│  Load        │  (K8s Ingress / Nginx)
│  Balancer    │  TLS termination
└──────┬───────┘
       │
┌──────▼───────────────────────────────────────┐
│  Public Subnet                                │
│  ┌──────────────┐                            │
│  │ API Gateway   │                            │
│  │ (YARP)        │ Rate limiting, auth check  │
│  └──────┬───────┘                            │
└─────────┼────────────────────────────────────┘
          │
┌─────────▼────────────────────────────────────┐
│  Private Subnet                               │
│  ┌──────┐ ┌──────┐ ┌────────┐ ┌───────────┐ │
│  │ API  │ │Worker│ │ RS.GE  │ │Background │ │
│  │ Pods │ │ Pods │ │ Worker │ │ Jobs      │ │
│  └──────┘ └──────┘ └────────┘ └───────────┘ │
│                                               │
│  ┌──────┐ ┌────────┐ ┌──────┐ ┌───────────┐ │
│  │Postgr│ │RabbitMQ│ │Redis │ │Monitoring │ │
│  │ SQL  │ │        │ │      │ │           │ │
│  └──────┘ └────────┘ └──────┘ └───────────┘ │
└───────────────────────────────────────────────┘
```

### 5.2 Network Policies

| Rule | From | To | Port | Protocol |
|------|------|-----|------|----------|
| API ingress | Load Balancer | API Pods | 8080 | HTTPS |
| API to DB | API Pods | PostgreSQL | 5432 | TCP (TLS) |
| API to Redis | API Pods | Redis | 6379 | TCP (TLS) |
| API to RabbitMQ | API/Worker Pods | RabbitMQ | 5672 | AMQP (TLS) |
| RS.GE Worker to Internet | RS.GE Worker Pod | services.rs.ge | 443 | HTTPS |
| Monitoring | Prometheus | All Pods | 9090 | HTTP |
| All other | * | * | * | **DENY** |

---

## 6. Audit & Compliance Logging

### 6.1 What Gets Logged

| Event Category | Examples | Retention |
|---------------|----------|-----------|
| Authentication | Login, logout, failed login, 2FA, token refresh | 2 years |
| Authorization | Permission denied, role changes | 2 years |
| Data Access | Read sensitive data (PII, financial) | 1 year |
| Data Modification | Create, update, delete any business entity | 10 years |
| RS.GE Communication | All requests/responses | 10 years |
| System Events | Config changes, deployment, errors | 1 year |
| Admin Actions | User creation, role assignment, settings change | 10 years |

### 6.2 Audit Log Integrity

- Append-only table (no UPDATE/DELETE permissions)
- Separate database user for audit writes (no delete privilege)
- Hash chain: each entry includes hash of previous entry
- Daily integrity verification job
- Independent backup with separate access controls

---

## 7. GDPR Compliance

### 7.1 Data Subject Rights

| Right | Implementation |
|-------|---------------|
| Right to Access | Export customer data endpoint (admin function) |
| Right to Rectification | Standard update operations |
| Right to Erasure | Anonymization (preserve transaction integrity, remove PII) |
| Right to Data Portability | JSON/CSV export |
| Consent Management | Explicit opt-in for SMS/email, recorded with timestamp |
| Breach Notification | Incident response procedure, 72-hour notification |

### 7.2 Data Minimization

- Collect only necessary PII
- Customer phone/email encrypted
- No unnecessary data retention beyond legal requirements
- Automatic anonymization of old customer data (configurable)

---

## 8. Device Management

| Feature | Implementation |
|---------|---------------|
| Device Registration | First login from new device requires additional verification |
| Device Fingerprinting | Browser/device hash stored with refresh token |
| Trusted Devices | User can mark devices as trusted (skip 2FA) |
| Device Revocation | Admin can revoke specific devices |
| IP Restrictions | Configurable per role (e.g., POS only from store IPs) |
| Session Visibility | Users can see all active sessions and revoke them |
