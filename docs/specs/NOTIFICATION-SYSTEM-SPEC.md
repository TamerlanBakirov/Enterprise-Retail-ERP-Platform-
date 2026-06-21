# Notification System Specification

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 21, 2026
**Status:** Draft

---

## 1. Overview

### 1.1 Objective
Design a multi-channel notification system that delivers timely, actionable alerts to users across In-App, Email, and SMS channels. The system must support Georgian language content, respect user notification preferences, and provide reliable delivery with tracking.

### 1.2 Notification Categories

| Category | Description | Priority | Default Channels |
|----------|-------------|----------|------------------|
| RS.GE Compliance | Invoice deadlines, waybill status, VAT reminders | Critical | InApp + Email + SMS |
| Inventory | Low stock alerts, stock discrepancies, transfer arrivals | High | InApp + Email |
| Approval Requests | New approvals, reminders, escalations | High | InApp + Email |
| System Alerts | Background job failures, security events, license expiry | Critical | InApp + Email |
| CRM | Campaign delivery status, customer events | Normal | InApp |
| Finance | Reconciliation reminders, period close, overdue AP/AR | Normal | InApp + Email |
| POS | Daily closing reminders, fiscal receipt failures | High | InApp |

---

## 2. Architecture

### 2.1 High-Level Flow

```
Event Source                  Notification Engine              Delivery
+------------------+         +---------------------+         +------------------+
| Domain Events    |         |                     |         |                  |
| (MediatR)        |-------->| Event Handlers      |         | In-App           |
|                  |         | (filter + enrich)   |-------->| (SignalR/DB)     |
+------------------+         |                     |         |                  |
                              | Template Engine     |         | Email            |
+------------------+         | (resolve content)   |-------->| (SMTP/SendGrid)  |
| Scheduled Jobs   |         |                     |         |                  |
| (Hangfire/Quartz)|-------->| Preference Check    |         | SMS              |
|                  |         | (user opt-in/out)   |-------->| (Magti/Silknet)  |
+------------------+         |                     |         |                  |
                              | Queue              |         +------------------+
+------------------+         | (RabbitMQ)          |
| Manual Triggers  |         |                     |
| (API calls)      |-------->|                     |
+------------------+         +---------------------+
```

### 2.2 Components

| Component | Technology | Role |
|-----------|------------|------|
| Event Handlers | MediatR `INotificationHandler` | Convert domain events to notification commands |
| Template Engine | Razor/Scriban templates | Render bilingual content (EN + KA) |
| Preference Service | Database-backed | Check user channel preferences and quiet hours |
| Queue | RabbitMQ | Decouple notification creation from delivery |
| In-App Delivery | SignalR + PostgreSQL | Real-time push + persistent storage |
| Email Delivery | SMTP / SendGrid | HTML email with Georgian character support |
| SMS Delivery | Magti SMS API / Silknet SMS Gateway | Georgian provider integration |
| Delivery Tracker | PostgreSQL | Track delivery status and retry failed messages |

---

## 3. Domain Model

### 3.1 Core Entities

```csharp
// Domain/Notifications/Notification.cs
public class Notification : BaseEntity
{
    public Guid RecipientUserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public NotificationPriority Priority { get; private set; }
    public string Title { get; private set; } = default!;
    public string? TitleKa { get; private set; }
    public string Body { get; private set; } = default!;
    public string? BodyKa { get; private set; }
    public string? ActionUrl { get; private set; }              // Deep link to relevant page
    public string? ActionLabel { get; private set; }
    public string? ReferenceType { get; private set; }          // "PurchaseOrder", "StockLevel", etc.
    public Guid? ReferenceId { get; private set; }
    public bool IsRead { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public bool IsDismissed { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? ExpiresAt { get; private set; }      // Auto-dismiss after this time

    public ICollection<NotificationDelivery> Deliveries { get; private set; }
        = new List<NotificationDelivery>();
}

public enum NotificationCategory
{
    RsGeCompliance,
    Inventory,
    ApprovalRequest,
    SystemAlert,
    Crm,
    Finance,
    Pos
}

public enum NotificationPriority
{
    Low,
    Normal,
    High,
    Critical
}

// Domain/Notifications/NotificationDelivery.cs
public class NotificationDelivery : BaseEntity
{
    public Guid NotificationId { get; private set; }
    public NotificationChannel Channel { get; private set; }
    public DeliveryStatus Status { get; private set; }
    public string? ExternalId { get; private set; }             // SMS message ID, email message ID
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Notification Notification { get; private set; } = default!;
}

public enum NotificationChannel
{
    InApp,
    Email,
    Sms
}

public enum DeliveryStatus
{
    Queued,
    Sending,
    Sent,
    Delivered,
    Failed,
    RetryPending
}

// Domain/Notifications/NotificationPreference.cs
public class NotificationPreference : BaseEntity
{
    public Guid UserId { get; private set; }
    public NotificationCategory Category { get; private set; }
    public bool InAppEnabled { get; private set; }
    public bool EmailEnabled { get; private set; }
    public bool SmsEnabled { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

// Domain/Notifications/NotificationTemplate.cs
public class NotificationTemplate : BaseEntity
{
    public string Code { get; private set; } = default!;            // "RSGE_INVOICE_DEADLINE"
    public string Name { get; private set; } = default!;
    public NotificationCategory Category { get; private set; }
    public string TitleTemplate { get; private set; } = default!;
    public string? TitleTemplateKa { get; private set; }
    public string BodyTemplate { get; private set; } = default!;     // Scriban template
    public string? BodyTemplateKa { get; private set; }
    public string? EmailSubjectTemplate { get; private set; }
    public string? EmailBodyTemplate { get; private set; }           // HTML Scriban template
    public string? EmailBodyTemplateKa { get; private set; }
    public string? SmsTemplate { get; private set; }                 // Plain text, max 160 chars
    public string? SmsTemplateKa { get; private set; }               // Georgian SMS, UCS-2 encoding
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
}

// Domain/Notifications/QuietHours.cs
public class QuietHours : BaseEntity
{
    public Guid UserId { get; private set; }
    public TimeOnly StartTime { get; private set; }             // e.g., 22:00
    public TimeOnly EndTime { get; private set; }               // e.g., 08:00
    public bool AppliesToSms { get; private set; }
    public bool AppliesToEmail { get; private set; }
    public bool AppliesToInApp { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

---

## 4. Message Templates

### 4.1 RS.GE Compliance Templates

#### RSGE_INVOICE_DEADLINE
```
Title (EN): "RS.GE Invoice Deadline Warning"
Title (KA): "RS.GE ინვოისის ვადის გაფრთხილება"

Body (EN): "Invoice for waybill {{waybill_number}} must be uploaded to RS.GE within
{{days_remaining}} days (deadline: {{deadline_date}}). Total: {{amount}} GEL."
Body (KA): "ზედნადები {{waybill_number}}-ის ინვოისი უნდა აიტვირთოს RS.GE-ზე
{{days_remaining}} დღეში (ვადა: {{deadline_date}}). თანხა: {{amount}} ლარი."

SMS (EN): "RS.GE Alert: Invoice for {{waybill_number}} due in {{days_remaining}} days. Amount: {{amount}} GEL."
SMS (KA): "RS.GE: ინვოისი {{waybill_number}} - {{days_remaining}} დღე დარჩა. {{amount}} ლარი."
```

#### RSGE_WAYBILL_STATUS
```
Title (EN): "Waybill Status Update"
Title (KA): "ზედნადების სტატუსის განახლება"

Body (EN): "Waybill {{waybill_number}} status changed to {{status}}.
Seller: {{seller_name}}, Buyer: {{buyer_name}}, Amount: {{amount}} GEL."
Body (KA): "ზედნადები {{waybill_number}} სტატუსი შეიცვალა: {{status}}.
გამყიდველი: {{seller_name}}, მყიდველი: {{buyer_name}}, თანხა: {{amount}} ლარი."
```

#### RSGE_VAT_REMINDER
```
Title (EN): "Monthly VAT Declaration Reminder"
Title (KA): "ყოველთვიური დღგ-ს დეკლარაციის შეხსენება"

Body (EN): "VAT declaration for {{period}} is due by {{due_date}}.
Output VAT: {{output_vat}} GEL, Input VAT: {{input_vat}} GEL, Net: {{net_vat}} GEL."
Body (KA): "{{period}} პერიოდის დღგ-ს დეკლარაცია უნდა წარდგეს {{due_date}}-მდე.
გამოსავალი დღგ: {{output_vat}} ლარი, შემოსავალი დღგ: {{input_vat}} ლარი, წმინდა: {{net_vat}} ლარი."
```

### 4.2 Inventory Templates

#### LOW_STOCK_ALERT
```
Title (EN): "Low Stock Alert"
Title (KA): "დაბალი მარაგის გაფრთხილება"

Body (EN): "{{product_count}} products are below minimum stock level in {{store_name}}:
{{#each products}}
- {{name}} ({{sku}}): {{current_stock}} / {{min_stock}} {{unit}}
{{/each}}"
Body (KA): "{{product_count}} პროდუქტის მარაგი მინიმუმზე ნაკლებია {{store_name}}-ში:
{{#each products}}
- {{name}} ({{sku}}): {{current_stock}} / {{min_stock}} {{unit}}
{{/each}}"

SMS (EN): "Low stock: {{product_count}} products below minimum at {{store_name}}. Check ERP for details."
SMS (KA): "დაბალი მარაგი: {{product_count}} პროდუქტი {{store_name}}-ში. შეამოწმეთ ERP."
```

#### STOCK_TRANSFER_ARRIVED
```
Title (EN): "Stock Transfer Arrived"
Title (KA): "მარაგის ტრანსფერი ჩამოვიდა"

Body (EN): "Transfer {{transfer_number}} from {{source_warehouse}} has arrived at
{{dest_warehouse}}. {{item_count}} items, total value: {{total_value}} GEL."
```

### 4.3 Approval Templates

#### APPROVAL_REQUESTED
```
Title (EN): "Approval Required: {{document_type}} {{document_number}}"
Title (KA): "საჭიროა დამტკიცება: {{document_type}} {{document_number}}"

Body (EN): "{{requester_name}} has submitted {{document_type}} {{document_number}}
for your approval.
Amount: {{amount}} GEL
Summary: {{summary}}
Step: {{step_name}} ({{step_order}} of {{total_steps}})"

Email Subject: "[Action Required] {{document_type}} {{document_number}} - Approval Needed"
```

#### APPROVAL_ESCALATED
```
Title (EN): "Escalated: {{document_type}} {{document_number}}"
Title (KA): "ესკალაცია: {{document_type}} {{document_number}}"

Body (EN): "{{document_type}} {{document_number}} has been escalated to you after
{{hours_overdue}} hours without action. Original approver: {{original_approver}}.
Amount: {{amount}} GEL."

SMS (EN): "URGENT: {{document_type}} {{document_number}} escalated. {{amount}} GEL. Review in ERP."
SMS (KA): "სასწრაფო: {{document_type}} {{document_number}} ესკალაცია. {{amount}} ლარი."
```

### 4.4 System Alert Templates

#### BACKGROUND_JOB_FAILURE
```
Title (EN): "Background Job Failed: {{job_name}}"
Title (KA): "ფონური ამოცანა ვერ შესრულდა: {{job_name}}"

Body (EN): "Background job {{job_name}} failed at {{failure_time}}.
Error: {{error_message}}
Retry count: {{retry_count}}
Action required: {{action}}"
```

#### LICENSE_EXPIRY_WARNING
```
Title (EN): "License Expiring Soon"
Title (KA): "ლიცენზიის ვადა იწურება"

Body (EN): "Your ERP license expires on {{expiry_date}} ({{days_remaining}} days remaining).
Please contact support to renew."
```

### 4.5 Finance Templates

#### RECONCILIATION_REMINDER
```
Title (EN): "Bank Reconciliation Overdue"
Title (KA): "საბანკო შერიგება ვადაგადაცილებულია"

Body (EN): "Bank account {{account_name}} ({{bank_name}}) has not been reconciled
for {{days_since_last}} days. Last reconciliation: {{last_date}}."
```

#### PERIOD_CLOSE_REMINDER
```
Title (EN): "Fiscal Period Close Reminder"
Title (KA): "ფისკალური პერიოდის დახურვის შეხსენება"

Body (EN): "Fiscal period {{period_name}} is still open. Please review and close
the period. {{pending_entries}} journal entries are pending posting."
```

---

## 5. Delivery Channels

### 5.1 In-App Notifications

#### 5.1.1 Real-Time Delivery
- **Technology:** SignalR WebSocket hub
- **Hub path:** `/hubs/notifications`
- **Events:** `ReceiveNotification`, `NotificationRead`, `NotificationCountUpdate`

#### 5.1.2 Persistence
- Notifications are stored in the `notifications` table.
- Unread notifications are displayed in the UI notification bell with a count badge.
- Notifications are retained for 90 days, then archived/deleted by a cleanup job.

#### 5.1.3 API Endpoints

```
GET    /api/v1/notifications
       ?category={NotificationCategory}
       &isRead={bool}
       &page={int}&pageSize={int}
       -> Paginated notification list for current user

GET    /api/v1/notifications/unread-count
       -> { count: int }

POST   /api/v1/notifications/{id}/read
       -> Mark as read

POST   /api/v1/notifications/read-all
       -> Mark all as read

POST   /api/v1/notifications/{id}/dismiss
       -> Dismiss (hide from list)

GET    /api/v1/notifications/preferences
       -> Get current user's notification preferences

PUT    /api/v1/notifications/preferences
       Body: { preferences: [{ category, inAppEnabled, emailEnabled, smsEnabled }] }
       -> Update preferences

GET    /api/v1/notifications/quiet-hours
PUT    /api/v1/notifications/quiet-hours
       Body: { startTime, endTime, appliesToSms, appliesToEmail, appliesToInApp }
```

### 5.2 Email Delivery

#### 5.2.1 Provider Configuration
- **Primary:** SendGrid (for reliability and deliverability)
- **Fallback:** Direct SMTP (for on-premise deployments)
- **Sender:** `noreply@{company-domain}` or configurable per category

#### 5.2.2 Email Format
- HTML emails with responsive design
- Georgian character support (UTF-8 encoding)
- Company branding (logo, colors) via configurable base template
- Unsubscribe link (for marketing-type emails, per Georgian data protection requirements)
- Plain-text alternative body

#### 5.2.3 Rate Limits
- Max 500 emails per minute (SendGrid rate limit)
- Max 10,000 emails per day (configurable)
- Batch sending for campaign-type notifications

### 5.3 SMS Delivery

#### 5.3.1 Georgian SMS Providers

| Provider | API Type | Coverage | Notes |
|----------|----------|----------|-------|
| **Magti** (primary) | HTTP REST API | All Georgian operators | Most popular provider |
| **Silknet** (fallback) | HTTP REST API | All Georgian operators | Backup provider |

#### 5.3.2 Integration

```csharp
// Infrastructure/Notifications/Sms/ISmsProvider.cs
public interface ISmsProvider
{
    Task<SmsResult> SendAsync(string phoneNumber, string message, CancellationToken ct);
    Task<SmsDeliveryStatus> CheckStatusAsync(string messageId, CancellationToken ct);
}

// Infrastructure/Notifications/Sms/MagtiSmsProvider.cs
public class MagtiSmsProvider : ISmsProvider
{
    // Magti API: POST https://api.magtigsm.ge/api/sms/send
    // Auth: API key header
    // Body: { "to": "+995...", "message": "...", "from": "ERP" }
}

// Infrastructure/Notifications/Sms/SilknetSmsProvider.cs
public class SilknetSmsProvider : ISmsProvider
{
    // Silknet API: POST https://sms.silknet.com/api/v1/send
    // Auth: Bearer token
    // Body: { "destination": "+995...", "text": "...", "sender": "ERP" }
}
```

#### 5.3.3 SMS Rules
- Georgian phone numbers: `+995 5XX XXX XXX` (mobile)
- SMS sender ID: configurable, max 11 alphanumeric characters (e.g., "GeorgiaERP")
- Character limits:
  - Latin (GSM 7-bit): 160 characters per segment
  - Georgian (UCS-2): 70 characters per segment
  - Long messages: automatically split into multi-part SMS
- SMS is reserved for Critical and High priority notifications only
- Daily SMS limit per user: 10 (configurable, to prevent spam)

#### 5.3.4 Failover
1. Attempt delivery via Magti (primary)
2. If Magti fails, retry via Silknet (fallback)
3. If both fail, mark as `Failed` and log for manual review

---

## 6. Delivery Rules

### 6.1 Priority-Based Routing

| Priority | InApp | Email | SMS | Behavior |
|----------|-------|-------|-----|----------|
| Critical | Always | Always | Always | Ignore quiet hours, ignore preferences |
| High | Always | Default on | If enabled | Respect quiet hours (queue for delivery after) |
| Normal | Always | If enabled | Never | Respect quiet hours |
| Low | Always | Never | Never | Respect quiet hours |

### 6.2 Deduplication Rules
- BR-NF-01: Do not send the same notification (same template + reference + recipient) within a configurable cooldown window (default: 1 hour).
- BR-NF-02: Aggregate low-stock alerts: instead of one notification per product, batch into a single notification listing all low-stock products for a store.
- BR-NF-03: RS.GE deadline warnings escalate: 7 days before (Normal), 3 days before (High), 1 day before (Critical).

### 6.3 Quiet Hours
- BR-NF-04: During quiet hours, notifications are queued and delivered when quiet hours end.
- BR-NF-05: Critical notifications bypass quiet hours.
- BR-NF-06: Default quiet hours: 22:00 - 08:00 Tbilisi time (UTC+4).

### 6.4 Retry Policy
- BR-NF-07: Failed email delivery: retry 3 times with exponential backoff (1 min, 5 min, 30 min).
- BR-NF-08: Failed SMS delivery: retry 2 times with 5-minute intervals, then failover to backup provider.
- BR-NF-09: After all retries are exhausted, mark as `Failed` and create a system alert for the admin.

---

## 7. Notification Triggers (Event Mappings)

### 7.1 RS.GE Compliance Events

| Trigger | Source | Template | Recipients | Priority |
|---------|--------|----------|------------|----------|
| Invoice deadline approaching (7 days) | `ComplianceWorker` scheduled job | `RSGE_INVOICE_DEADLINE` | Compliance officers | Normal |
| Invoice deadline approaching (3 days) | `ComplianceWorker` scheduled job | `RSGE_INVOICE_DEADLINE` | Compliance officers + Store manager | High |
| Invoice deadline approaching (1 day) | `ComplianceWorker` scheduled job | `RSGE_INVOICE_DEADLINE` | All finance team | Critical |
| Waybill status changed | `WaybillConfirmedEvent` domain event | `RSGE_WAYBILL_STATUS` | Relevant store team | Normal |
| RS.GE API failure | Infrastructure error | `RSGE_API_FAILURE` | System admins | Critical |
| Monthly VAT declaration due | 20th of each month | `RSGE_VAT_REMINDER` | Finance team | High |

### 7.2 Inventory Events

| Trigger | Source | Template | Recipients | Priority |
|---------|--------|----------|------------|----------|
| Stock below minimum | `StockAdjustedEvent` handler | `LOW_STOCK_ALERT` | Store manager + Procurement | High |
| Stock below critical (0) | `StockAdjustedEvent` handler | `STOCK_CRITICAL` | Store manager + Regional manager | Critical |
| Transfer order arrived | `TransferOrder.Receive()` | `STOCK_TRANSFER_ARRIVED` | Destination warehouse manager | Normal |
| Stock count discrepancy | `StockCount` completed | `STOCK_DISCREPANCY` | Store manager + Finance | High |

### 7.3 Approval Events

| Trigger | Source | Template | Recipients | Priority |
|---------|--------|----------|------------|----------|
| New approval request | `ApprovalRequestedEvent` | `APPROVAL_REQUESTED` | Assigned approver(s) | High |
| Approval reminder (50% time) | `ApprovalReminderJob` | `APPROVAL_REMINDER` | Current approver | Normal |
| Approval escalation | `ApprovalEscalatedEvent` | `APPROVAL_ESCALATED` | Escalation approver | Critical |
| Approval decision | `ApprovalDecisionEvent` | `APPROVAL_DECIDED` | Requester | Normal |

### 7.4 System Events

| Trigger | Source | Template | Recipients | Priority |
|---------|--------|----------|------------|----------|
| Background job failure | Job infrastructure | `BACKGROUND_JOB_FAILURE` | System admins | Critical |
| License expiry (30 days) | Daily check | `LICENSE_EXPIRY_WARNING` | Admins | Normal |
| License expiry (7 days) | Daily check | `LICENSE_EXPIRY_WARNING` | Admins | High |
| Security: failed login attempts | Identity module | `SECURITY_ALERT` | User + Security admin | High |
| Database backup failure | Backup job | `BACKUP_FAILURE` | System admins | Critical |

---

## 8. Business Rules Summary

| Rule | Description |
|------|-------------|
| BR-NF-01 | Deduplication: same notification (template + reference + recipient) has a 1-hour cooldown |
| BR-NF-02 | Low-stock alerts are batched per store per check interval |
| BR-NF-03 | RS.GE deadline warnings escalate in priority as the deadline approaches |
| BR-NF-04 | Quiet hours queue non-critical notifications for later delivery |
| BR-NF-05 | Critical notifications bypass quiet hours and user preferences |
| BR-NF-06 | Default quiet hours: 22:00 - 08:00 UTC+4 |
| BR-NF-07 | Email retry: 3 attempts with exponential backoff |
| BR-NF-08 | SMS retry: 2 attempts then failover to backup provider |
| BR-NF-09 | Failed deliveries after all retries create admin alerts |
| BR-NF-10 | SMS daily limit per user: 10 messages (configurable) |
| BR-NF-11 | Georgian SMS uses UCS-2 encoding (70 chars per segment) |
| BR-NF-12 | Notifications expire and auto-dismiss after their ExpiresAt date |
| BR-NF-13 | In-app notifications are retained for 90 days |
| BR-NF-14 | Users can configure per-category channel preferences |
| BR-NF-15 | Marketing SMS/Email requires customer consent (Customer.ConsentSms/ConsentEmail) |

---

## 9. Required Permissions

| Permission | Description |
|------------|-------------|
| `Notification.View` | View own notifications (all users have this by default) |
| `Notification.Preferences.Manage` | Update own notification preferences |
| `Notification.Template.View` | View notification templates |
| `Notification.Template.Manage` | Create, edit notification templates (admin) |
| `Notification.Send` | Manually send notifications (admin) |
| `Notification.Admin` | View all users' notification delivery status, retry failed |

---

## 10. API Endpoints (Complete)

### 10.1 User-Facing

```
GET    /api/v1/notifications
       ?category={NotificationCategory}
       &isRead={bool}
       &priority={NotificationPriority}
       &from={ISO8601}
       &to={ISO8601}
       &page={int}&pageSize={int}

GET    /api/v1/notifications/unread-count

GET    /api/v1/notifications/{id}

POST   /api/v1/notifications/{id}/read
POST   /api/v1/notifications/read-all
POST   /api/v1/notifications/{id}/dismiss

GET    /api/v1/notifications/preferences
PUT    /api/v1/notifications/preferences

GET    /api/v1/notifications/quiet-hours
PUT    /api/v1/notifications/quiet-hours
```

### 10.2 Admin

```
GET    /api/v1/notifications/templates
GET    /api/v1/notifications/templates/{id}
POST   /api/v1/notifications/templates
PUT    /api/v1/notifications/templates/{id}

POST   /api/v1/notifications/send
       Body: { recipientUserIds, templateCode, templateData, channels }
       -> Manually trigger a notification

GET    /api/v1/notifications/delivery-log
       ?status={DeliveryStatus}
       &channel={NotificationChannel}
       &from={ISO8601}
       &to={ISO8601}
       -> Delivery audit log

POST   /api/v1/notifications/delivery/{id}/retry
       -> Retry a failed delivery
```

---

## 11. SignalR Hub Specification

### 11.1 Hub Contract

```csharp
// Hubs/NotificationHub.cs
public class NotificationHub : Hub
{
    // Client methods (server -> client)
    // Clients.User(userId).SendAsync("ReceiveNotification", notification)
    // Clients.User(userId).SendAsync("UpdateUnreadCount", count)

    // Server methods (client -> server)
    public Task MarkAsRead(Guid notificationId);
    public Task DismissNotification(Guid notificationId);
    public Task GetUnreadCount();
}
```

### 11.2 Connection Management
- Authentication: JWT token passed as query parameter on connection
- Reconnection: automatic with exponential backoff (1s, 2s, 4s, 8s, max 30s)
- Group membership: each user is in a group named `user:{userId}`
- Connection tracking: active connections stored in Redis for multi-instance support

### 11.3 Client Integration

```typescript
// Frontend: src/shared/hooks/useNotifications.ts
// SignalR connection management
// Auto-reconnect on disconnect
// Unread count state management
// Toast display for new notifications
```

---

## 12. Database Tables

### 12.1 New Tables
- `notifications` -- notification records per user
- `notification_deliveries` -- per-channel delivery tracking
- `notification_preferences` -- user channel preferences per category
- `notification_templates` -- message templates (bilingual)
- `quiet_hours` -- user quiet hour settings

### 12.2 Indexes
- `idx_notifications_user_unread` on `notifications(recipient_user_id, is_read, created_at DESC)` -- inbox query
- `idx_notifications_user_category` on `notifications(recipient_user_id, category)` -- filtered inbox
- `idx_notification_deliveries_status` on `notification_deliveries(status, channel)` -- retry queue
- `idx_notification_deliveries_notification` on `notification_deliveries(notification_id)` -- delivery lookup
- `idx_notifications_expires` on `notifications(expires_at)` where `expires_at IS NOT NULL` -- cleanup job

### 12.3 Seed Data
- Default notification templates for all categories (EN + KA)
- Default notification preferences (all categories: InApp = true)
- Default quiet hours: 22:00 - 08:00

---

## 13. Background Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `NotificationDeliveryWorker` | Continuous (queue consumer) | Process queued notifications from RabbitMQ |
| `NotificationRetryJob` | Every 5 minutes | Retry failed deliveries (up to max retries) |
| `NotificationCleanupJob` | Daily at 03:00 UTC+4 | Delete expired and dismissed notifications older than 90 days |
| `QuietHoursFlushJob` | Every 15 minutes | Deliver queued notifications when quiet hours end |
| `RsGeDeadlineCheckJob` | Daily at 09:00 UTC+4 | Check for approaching RS.GE deadlines and send warnings |
| `LowStockCheckJob` | Every 4 hours | Check stock levels against minimums and send alerts |
| `SmsDeliveryStatusJob` | Every 10 minutes | Check SMS delivery status with provider APIs |

---

## 14. Configuration

### 14.1 Application Settings

```json
{
    "Notifications": {
        "InApp": {
            "RetentionDays": 90,
            "MaxUnreadCount": 999,
            "SignalRHubPath": "/hubs/notifications"
        },
        "Email": {
            "Provider": "SendGrid",
            "ApiKey": "{{from-vault}}",
            "FromAddress": "noreply@example.ge",
            "FromName": "Georgia ERP",
            "MaxPerMinute": 500,
            "MaxPerDay": 10000,
            "Smtp": {
                "Host": "smtp.example.ge",
                "Port": 587,
                "UseSsl": true,
                "Username": "{{from-vault}}",
                "Password": "{{from-vault}}"
            }
        },
        "Sms": {
            "PrimaryProvider": "Magti",
            "FallbackProvider": "Silknet",
            "SenderId": "GeorgiaERP",
            "MaxPerUserPerDay": 10,
            "Magti": {
                "ApiUrl": "https://api.magtigsm.ge/api/sms",
                "ApiKey": "{{from-vault}}"
            },
            "Silknet": {
                "ApiUrl": "https://sms.silknet.com/api/v1",
                "BearerToken": "{{from-vault}}"
            }
        },
        "QuietHours": {
            "DefaultStart": "22:00",
            "DefaultEnd": "08:00",
            "Timezone": "Asia/Tbilisi"
        },
        "Deduplication": {
            "CooldownMinutes": 60
        },
        "Retry": {
            "EmailMaxRetries": 3,
            "EmailBackoffMinutes": [1, 5, 30],
            "SmsMaxRetries": 2,
            "SmsRetryMinutes": 5
        }
    }
}
```

---

## 15. Testing Strategy

### 15.1 Unit Tests
- Template rendering with various data inputs (EN + KA)
- Priority-based channel routing logic
- Deduplication cooldown enforcement
- Quiet hours calculation across midnight boundary
- SMS character count and segment splitting (Latin vs Georgian)
- Retry policy (backoff timing, max retries)

### 15.2 Integration Tests
- End-to-end: domain event -> notification created -> in-app delivery -> SignalR push
- Email delivery via SMTP test server (Papercut/MailHog)
- SMS delivery via provider mock (HTTP mock server)
- Preference filtering: notification sent only on enabled channels
- Quiet hours: notification queued during quiet hours, delivered after
- Failover: primary SMS provider fails, fallback provider delivers

### 15.3 Performance Tests
- 1,000 concurrent SignalR connections: all receive real-time notifications
- Batch notification to 5,000 users: queued within 10 seconds
- Notification inbox query: < 100ms for users with 500+ notifications

---

## 16. Acceptance Criteria

- AC-NF-01: An RS.GE invoice deadline approaching 3 days sends an InApp notification, an Email, and an SMS to the compliance officer.
- AC-NF-02: A low-stock alert lists all products below minimum for the store in a single notification.
- AC-NF-03: A user with quiet hours set to 22:00-08:00 does not receive Email/SMS during that window (except Critical priority).
- AC-NF-04: A Critical system alert (background job failure) is delivered immediately regardless of quiet hours and preferences.
- AC-NF-05: Marking a notification as read via the API immediately updates the unread count visible in the UI.
- AC-NF-06: An approval escalation notification is delivered via InApp + Email + SMS (Critical priority).
- AC-NF-07: Failed SMS delivery retries via the primary provider, then fails over to the backup provider.
- AC-NF-08: Notification templates render correctly in both English and Georgian.
- AC-NF-09: The same low-stock alert for the same store is not sent more than once within the cooldown window.
- AC-NF-10: Users can disable Email notifications for the "Inventory" category and still receive InApp notifications.
- AC-NF-11: Admin can view the delivery log showing sent, failed, and retried notifications with timestamps.
- AC-NF-12: Notifications older than 90 days are automatically cleaned up.
