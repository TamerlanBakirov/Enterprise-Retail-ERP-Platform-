# CRM Module Completion Specification

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 21, 2026
**Status:** Draft
**Module Completion Target:** 70% -> 100%

---

## 1. Current State Assessment

### 1.1 Existing Entities

| Entity | File | Status |
|--------|------|--------|
| `Customer` | `Domain/CRM/Customer.cs` | Complete -- basic profile, loyalty points, visit tracking |
| `LoyaltyTransaction` | `Domain/CRM/LoyaltyTransaction.cs` | Complete -- earn, redeem, adjust, expire |

### 1.2 Existing Application Handlers

| Handler | Status |
|---------|--------|
| `CreateCustomerCommand` | Complete -- with phone uniqueness check |
| `EarnLoyaltyPointsCommand` | Complete -- basic point earning |
| `RedeemLoyaltyPointsCommand` | Complete -- with balance validation |
| `GetCustomersQuery` | Complete -- search, pagination, filtering |
| `CreateCustomerCommandValidator` | Complete -- FluentValidation |

### 1.3 Existing Customer Properties
- CustomerNumber, FirstName/LastName (EN + KA), CompanyName, TIN
- Phone, Email, DateOfBirth, Gender
- LoyaltyCardNumber, LoyaltyTier, LoyaltyPoints
- TotalPurchases, TotalVisits, LastVisitAt
- ConsentSms, ConsentEmail
- IsActive

### 1.4 Gaps Identified
1. **No customer segmentation** -- no way to group customers by behavior, demographics, or value
2. **No loyalty rules engine** -- points are earned/redeemed manually with no configurable rules
3. **No communication history** -- no record of messages, interactions, or outreach
4. **No credit management** -- no credit limits, terms, or outstanding balance tracking
5. **No purchase analytics** -- basic TotalPurchases exists but no category/product/time analysis
6. **No customer address management** -- no structured address entity
7. **No customer notes/tags** -- no free-form interaction tracking

---

## 2. Feature Specifications

### 2.1 Customer Segmentation

#### 2.1.1 Description
Enable dynamic and static segmentation of customers based on demographics, purchase behavior, loyalty status, and custom criteria. Segments drive targeted promotions, loyalty rules, and communication campaigns.

#### 2.1.2 New Entities

```csharp
// Domain/CRM/CustomerSegment.cs
public class CustomerSegment : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string? Description { get; private set; }
    public SegmentType SegmentType { get; private set; }
    public string? RulesJson { get; private set; }          // JSON rules for dynamic segments
    public int CustomerCount { get; private set; }           // Cached count, refreshed periodically
    public bool IsActive { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? LastEvaluatedAt { get; private set; }

    public ICollection<CustomerSegmentMember> Members { get; private set; }
        = new List<CustomerSegmentMember>();
}

public enum SegmentType
{
    Static,             // Manually assigned members
    Dynamic             // Rule-based, re-evaluated periodically
}

// Domain/CRM/CustomerSegmentMember.cs
public class CustomerSegmentMember : BaseEntity
{
    public Guid SegmentId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTimeOffset AddedAt { get; private set; }
    public string? AddedReason { get; private set; }        // "Manual", "Rule: High Value"

    public CustomerSegment Segment { get; private set; } = default!;
    public Customer Customer { get; private set; } = default!;
}
```

#### 2.1.3 Dynamic Segment Rule Schema

```json
{
  "operator": "AND",
  "conditions": [
    {
      "field": "TotalPurchases",
      "operator": "greaterThan",
      "value": 5000
    },
    {
      "field": "TotalVisits",
      "operator": "greaterThan",
      "value": 10
    },
    {
      "field": "LastVisitAt",
      "operator": "withinDays",
      "value": 90
    },
    {
      "field": "LoyaltyTier",
      "operator": "in",
      "value": ["Gold", "Platinum"]
    }
  ]
}
```

**Supported Fields:**
| Field | Type | Operators |
|-------|------|-----------|
| `TotalPurchases` | decimal | greaterThan, lessThan, between, equals |
| `TotalVisits` | int | greaterThan, lessThan, between, equals |
| `LastVisitAt` | date | withinDays, before, after |
| `LoyaltyTier` | string | equals, in |
| `LoyaltyPoints` | int | greaterThan, lessThan, between |
| `Gender` | string | equals, in |
| `Age` | int (calculated) | greaterThan, lessThan, between |
| `City` | string | equals, in |
| `HasEmail` | bool | equals |
| `HasPhone` | bool | equals |
| `DaysSinceLastVisit` | int (calculated) | greaterThan, lessThan |
| `AverageTransactionValue` | decimal (calculated) | greaterThan, lessThan, between |

#### 2.1.4 API Endpoints

```
GET    /api/v1/crm/segments
       ?isActive={bool}
       &segmentType={Static|Dynamic}
       -> List all segments with customer counts

GET    /api/v1/crm/segments/{id}
       -> Segment detail with member list (paginated)

POST   /api/v1/crm/segments
       Body: { code, name, nameKa, description, segmentType, rules }
       -> Create a segment

PUT    /api/v1/crm/segments/{id}
       Body: { name, nameKa, description, rules }
       -> Update segment

DELETE /api/v1/crm/segments/{id}
       -> Soft-delete segment

POST   /api/v1/crm/segments/{id}/evaluate
       -> Force re-evaluation of a dynamic segment

POST   /api/v1/crm/segments/{id}/members
       Body: { customerIds: [] }
       -> Add customers to a static segment

DELETE /api/v1/crm/segments/{segmentId}/members/{customerId}
       -> Remove customer from a static segment

GET    /api/v1/crm/customers/{customerId}/segments
       -> List segments a customer belongs to
```

#### 2.1.5 Business Rules
- BR-SG-01: Segment codes must be unique and uppercase alphanumeric (max 20 chars).
- BR-SG-02: Dynamic segments are re-evaluated by a background job every 24 hours. Manual re-evaluation is also available.
- BR-SG-03: Static segment membership is managed manually through the API.
- BR-SG-04: Dynamic segment rules are validated on creation; invalid field names or operators are rejected.
- BR-SG-05: A customer can belong to multiple segments simultaneously.
- BR-SG-06: Segment customer count is cached and updated after each evaluation.
- BR-SG-07: Deleting a segment removes all membership records.
- BR-SG-08: Maximum 100 conditions per segment rule set.

#### 2.1.6 Acceptance Criteria
- AC-SG-01: Creating a dynamic segment with rules "TotalPurchases > 5000 AND TotalVisits > 10" correctly identifies matching customers.
- AC-SG-02: Re-evaluating a dynamic segment adds new qualifying customers and removes those who no longer qualify.
- AC-SG-03: Static segment allows manual add/remove of customers.
- AC-SG-04: Customer detail page shows all segments the customer belongs to.
- AC-SG-05: Segment evaluation for 50,000 customers completes within 30 seconds.

---

### 2.2 Loyalty Rules Engine

#### 2.2.1 Description
Replace the current manual point earning/redemption with a configurable rules engine that automatically calculates points earned on purchases, manages tier progression, handles expiration, and defines redemption rates.

#### 2.2.2 New Entities

```csharp
// Domain/CRM/LoyaltyProgram.cs
public class LoyaltyProgram : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string Currency { get; private set; } = "GEL";
    public decimal BaseEarnRate { get; private set; }           // Points per 1 GEL spent (e.g., 1.0)
    public decimal BaseRedemptionRate { get; private set; }     // GEL value per point (e.g., 0.01)
    public int PointExpirationMonths { get; private set; }      // 0 = never expire
    public int MinRedemptionPoints { get; private set; }        // Minimum points to redeem
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ICollection<LoyaltyTier> Tiers { get; private set; }
        = new List<LoyaltyTier>();
    public ICollection<LoyaltyEarnRule> EarnRules { get; private set; }
        = new List<LoyaltyEarnRule>();
}

// Domain/CRM/LoyaltyTier.cs
public class LoyaltyTier : BaseEntity
{
    public Guid LoyaltyProgramId { get; private set; }
    public string Name { get; private set; } = default!;        // "Bronze", "Silver", "Gold", "Platinum"
    public string? NameKa { get; private set; }
    public int MinPoints { get; private set; }                   // Threshold to enter this tier
    public int? MaxPoints { get; private set; }                  // null = no upper limit (top tier)
    public decimal EarnMultiplier { get; private set; }          // e.g., 1.0, 1.5, 2.0
    public decimal RedemptionMultiplier { get; private set; }    // e.g., 1.0, 1.0, 1.2
    public decimal? DiscountPercent { get; private set; }        // Automatic discount for tier members
    public string? Benefits { get; private set; }                // JSON list of benefits
    public int SortOrder { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public LoyaltyProgram Program { get; private set; } = default!;
}

// Domain/CRM/LoyaltyEarnRule.cs
public class LoyaltyEarnRule : BaseEntity
{
    public Guid LoyaltyProgramId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public LoyaltyEarnRuleType RuleType { get; private set; }
    public decimal Multiplier { get; private set; }              // Points multiplier (e.g., 2.0 = double points)
    public Guid? CategoryId { get; private set; }                // Specific category (null = all)
    public Guid? ProductId { get; private set; }                 // Specific product (null = all)
    public Guid? StoreId { get; private set; }                   // Specific store (null = all)
    public Guid? SegmentId { get; private set; }                 // Specific segment (null = all)
    public DateTimeOffset? ValidFrom { get; private set; }
    public DateTimeOffset? ValidTo { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }                    // Higher priority rules apply first
    public DateTimeOffset CreatedAt { get; private set; }

    public LoyaltyProgram Program { get; private set; } = default!;
}

public enum LoyaltyEarnRuleType
{
    CategoryMultiplier,     // Extra points for specific categories
    ProductMultiplier,      // Extra points for specific products
    StoreMultiplier,        // Extra points at specific stores
    SegmentBonus,           // Extra points for specific customer segments
    TimeBonus,              // Extra points during specific time windows
    ThresholdBonus          // Extra points when transaction exceeds a threshold
}

// Domain/CRM/LoyaltyPointExpiry.cs
public class LoyaltyPointExpiry : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public int Points { get; private set; }
    public DateTimeOffset EarnedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public int RemainingPoints { get; private set; }
    public bool IsExpired { get; private set; }
    public DateTimeOffset? ExpiredAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
}
```

#### 2.2.3 API Endpoints

```
GET    /api/v1/crm/loyalty-programs
POST   /api/v1/crm/loyalty-programs
PUT    /api/v1/crm/loyalty-programs/{id}

GET    /api/v1/crm/loyalty-programs/{id}/tiers
POST   /api/v1/crm/loyalty-programs/{id}/tiers
PUT    /api/v1/crm/loyalty-programs/{programId}/tiers/{tierId}
DELETE /api/v1/crm/loyalty-programs/{programId}/tiers/{tierId}

GET    /api/v1/crm/loyalty-programs/{id}/earn-rules
POST   /api/v1/crm/loyalty-programs/{id}/earn-rules
PUT    /api/v1/crm/loyalty-programs/{programId}/earn-rules/{ruleId}
DELETE /api/v1/crm/loyalty-programs/{programId}/earn-rules/{ruleId}

POST   /api/v1/crm/loyalty/calculate
       Body: { customerId, transactionTotal, storeId, lineItems: [{productId, categoryId, amount}] }
       -> Preview points to be earned (for POS display)

POST   /api/v1/crm/loyalty/earn
       Body: { customerId, transactionId, transactionTotal, storeId, lineItems }
       -> Execute point earning (called after POS transaction completes)

POST   /api/v1/crm/loyalty/redeem
       Body: { customerId, points, transactionId }
       -> Redeem points (with tier-based rate)

GET    /api/v1/crm/customers/{id}/loyalty-summary
       -> Tier status, points balance, expiring points, tier progress

POST   /api/v1/crm/loyalty/expire-points
       -> Background job endpoint: expire points past their expiration date
```

#### 2.2.4 Business Rules
- BR-LY-01: Points earned = TransactionTotal * BaseEarnRate * TierMultiplier * max(applicable EarnRule multipliers).
- BR-LY-02: Earn rules are evaluated in priority order. The highest-priority matching rule's multiplier is applied (rules do not stack unless configured).
- BR-LY-03: Tier assignment is recalculated after each point-earning transaction. Tier upgrades are immediate; tier downgrades happen only on annual review.
- BR-LY-04: Points expire on a FIFO basis (oldest points are consumed first on redemption).
- BR-LY-05: Point expiration batch job runs daily. Customers with expiring points within 30 days receive a notification.
- BR-LY-06: Minimum redemption threshold must be met (e.g., minimum 100 points).
- BR-LY-07: Points earned on returned transactions are automatically reversed.
- BR-LY-08: The loyalty calculation endpoint (for POS preview) must respond within 200ms.
- BR-LY-09: Only one loyalty program can be active at a time.
- BR-LY-10: Tier benefits (JSON) can include: free_delivery, birthday_discount, early_access, priority_support.

#### 2.2.5 Acceptance Criteria
- AC-LY-01: A Gold-tier customer purchasing 100 GEL of goods earns correct points with tier multiplier applied.
- AC-LY-02: A 2x category multiplier earn rule for "Electronics" correctly doubles points for electronics purchases only.
- AC-LY-03: Customer tier upgrades from Silver to Gold when crossing the Gold tier threshold.
- AC-LY-04: Points earned 12 months ago are automatically expired by the background job.
- AC-LY-05: Redeeming 500 points deducts from the oldest earned points first (FIFO).
- AC-LY-06: Returning a purchased item reverses the loyalty points earned on that transaction.
- AC-LY-07: POS loyalty preview returns within 200ms for a 10-item transaction.

---

### 2.3 Communication History

#### 2.3.1 Description
Track all communications with customers across channels (SMS, Email, Phone, In-store), supporting campaign tracking and regulatory compliance for marketing consent.

#### 2.3.2 New Entities

```csharp
// Domain/CRM/Communication.cs
public class Communication : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public CommunicationChannel Channel { get; private set; }
    public CommunicationDirection Direction { get; private set; }
    public CommunicationType CommunicationType { get; private set; }
    public string? Subject { get; private set; }
    public string? Content { get; private set; }
    public string? ContentKa { get; private set; }
    public string? TemplateName { get; private set; }
    public CommunicationStatus Status { get; private set; }
    public string? ExternalId { get; private set; }             // SMS provider message ID, email message ID
    public Guid? CampaignId { get; private set; }
    public string? FailureReason { get; private set; }
    public DateTimeOffset? DeliveredAt { get; private set; }
    public DateTimeOffset? ReadAt { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
    public Campaign? Campaign { get; private set; }
}

public enum CommunicationChannel
{
    Sms,
    Email,
    Phone,
    InApp,
    InStore
}

public enum CommunicationDirection
{
    Outbound,
    Inbound
}

public enum CommunicationType
{
    Marketing,          // Promotional messages (requires consent)
    Transactional,      // Order confirmations, receipts (no consent required)
    Service,            // Support interactions
    System              // System-generated alerts
}

public enum CommunicationStatus
{
    Pending,
    Sent,
    Delivered,
    Failed,
    Bounced,
    Read
}

// Domain/CRM/Campaign.cs
public class Campaign : BaseEntity
{
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string? Description { get; private set; }
    public CommunicationChannel Channel { get; private set; }
    public Guid? SegmentId { get; private set; }                 // Target segment
    public string? TemplateContent { get; private set; }
    public string? TemplateContentKa { get; private set; }
    public CampaignStatus Status { get; private set; }
    public DateTimeOffset? ScheduledAt { get; private set; }
    public DateTimeOffset? SentAt { get; private set; }
    public int TotalRecipients { get; private set; }
    public int DeliveredCount { get; private set; }
    public int FailedCount { get; private set; }
    public int ReadCount { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public CustomerSegment? Segment { get; private set; }
    public ICollection<Communication> Communications { get; private set; }
        = new List<Communication>();
}

public enum CampaignStatus
{
    Draft,
    Scheduled,
    Sending,
    Completed,
    Cancelled
}

// Domain/CRM/CustomerNote.cs
public class CustomerNote : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public string NoteType { get; private set; } = default!;    // "General", "Complaint", "Feedback", "Follow-up"
    public string Content { get; private set; } = default!;
    public bool IsPinned { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
}
```

#### 2.3.3 API Endpoints

```
GET    /api/v1/crm/customers/{customerId}/communications
       ?channel={Sms|Email|Phone|InApp|InStore}
       &direction={Outbound|Inbound}
       &from={ISO8601}
       &to={ISO8601}
       &page={int}&pageSize={int}
       -> Paginated communication history

POST   /api/v1/crm/customers/{customerId}/communications
       Body: { channel, direction, communicationType, subject, content, contentKa }
       -> Log a communication (manual entry for phone/in-store)

GET    /api/v1/crm/campaigns
       ?status={Draft|Scheduled|Sending|Completed}
POST   /api/v1/crm/campaigns
PUT    /api/v1/crm/campaigns/{id}
POST   /api/v1/crm/campaigns/{id}/send
       -> Execute campaign (sends to segment members)
POST   /api/v1/crm/campaigns/{id}/cancel
GET    /api/v1/crm/campaigns/{id}/stats
       -> Campaign delivery statistics

GET    /api/v1/crm/customers/{customerId}/notes
POST   /api/v1/crm/customers/{customerId}/notes
PUT    /api/v1/crm/customers/{customerId}/notes/{noteId}
DELETE /api/v1/crm/customers/{customerId}/notes/{noteId}
POST   /api/v1/crm/customers/{customerId}/notes/{noteId}/pin
```

#### 2.3.4 Business Rules
- BR-CM-01: Marketing communications require `ConsentSms = true` (for SMS) or `ConsentEmail = true` (for Email). Transactional messages do not require consent.
- BR-CM-02: Each communication is logged with full audit trail (who sent, when, delivery status).
- BR-CM-03: Campaign sending is asynchronous. Messages are queued via RabbitMQ and processed by a background worker.
- BR-CM-04: Campaign sending respects rate limits: max 100 SMS/minute, max 500 Email/minute (configurable).
- BR-CM-05: Failed communications are retried up to 3 times with exponential backoff.
- BR-CM-06: SMS content must not exceed 160 characters for single SMS (Georgian Unicode may require UCS-2 encoding, limiting to 70 characters per segment).
- BR-CM-07: Campaign statistics are updated in real-time as delivery confirmations arrive.
- BR-CM-08: Customers who have opted out are automatically excluded from campaigns, even if they are in the target segment.
- BR-CM-09: Customer notes are ordered by creation date with pinned notes shown first.

#### 2.3.5 Acceptance Criteria
- AC-CM-01: Sending an SMS campaign to a segment of 1,000 customers queues all messages and begins delivery.
- AC-CM-02: Customers without SMS consent are excluded from SMS campaigns.
- AC-CM-03: Communication history for a customer shows all interactions across all channels, sorted by date.
- AC-CM-04: Campaign statistics accurately reflect delivered, failed, and read counts.
- AC-CM-05: Logging a phone call creates a record in the communication history with Direction = Inbound and Channel = Phone.
- AC-CM-06: Customer notes can be created, pinned, and deleted with proper audit trail.

---

### 2.4 Credit Management

#### 2.4.1 Description
Manage customer credit accounts with configurable credit limits, payment terms, and outstanding balance tracking. Integrates with POS for credit sales and with Finance for accounts receivable.

#### 2.4.2 New Entities

```csharp
// Domain/CRM/CustomerCredit.cs
public class CustomerCredit : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public decimal CreditLimit { get; private set; }
    public decimal AvailableCredit { get; private set; }
    public decimal OutstandingBalance { get; private set; }
    public decimal OverdueBalance { get; private set; }
    public int PaymentTermDays { get; private set; }            // Net 15, Net 30, etc.
    public CreditStatus Status { get; private set; }
    public string? ApprovalNotes { get; private set; }
    public Guid? ApprovedBy { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? LastPaymentAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
    public ICollection<CreditTransaction> Transactions { get; private set; }
        = new List<CreditTransaction>();
}

public enum CreditStatus
{
    PendingApproval,
    Active,
    Suspended,          // Temporarily suspended (overdue)
    Closed
}

// Domain/CRM/CreditTransaction.cs
public class CreditTransaction : BaseEntity
{
    public Guid CustomerCreditId { get; private set; }
    public CreditTransactionType TransactionType { get; private set; }
    public decimal Amount { get; private set; }
    public decimal BalanceAfter { get; private set; }
    public string? ReferenceType { get; private set; }          // "PosTransaction", "Payment", "Adjustment"
    public Guid? ReferenceId { get; private set; }
    public DateTimeOffset? DueDate { get; private set; }
    public string? Description { get; private set; }
    public Guid CreatedBy { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public CustomerCredit CustomerCredit { get; private set; } = default!;
}

public enum CreditTransactionType
{
    Charge,             // New credit sale
    Payment,            // Customer payment received
    Adjustment,         // Manual adjustment
    WriteOff,           // Bad debt write-off
    Refund              // Credit refund
}
```

#### 2.4.3 API Endpoints

```
GET    /api/v1/crm/customers/{customerId}/credit
       -> Get credit account details

POST   /api/v1/crm/customers/{customerId}/credit
       Body: { creditLimit, paymentTermDays, approvalNotes }
       -> Create credit account (status: PendingApproval)

PUT    /api/v1/crm/customers/{customerId}/credit
       Body: { creditLimit, paymentTermDays }
       -> Update credit terms

POST   /api/v1/crm/customers/{customerId}/credit/approve
       -> Approve credit account

POST   /api/v1/crm/customers/{customerId}/credit/suspend
       -> Suspend credit account

POST   /api/v1/crm/customers/{customerId}/credit/charge
       Body: { amount, referenceType, referenceId, description }
       -> Record a credit sale

POST   /api/v1/crm/customers/{customerId}/credit/payment
       Body: { amount, description }
       -> Record a payment received

GET    /api/v1/crm/customers/{customerId}/credit/transactions
       ?from={ISO8601}&to={ISO8601}
       &page={int}&pageSize={int}
       -> Credit transaction history

GET    /api/v1/crm/credit/overdue
       ?daysOverdue={int}
       -> List all overdue credit accounts

GET    /api/v1/crm/credit/summary
       -> Aggregate credit portfolio summary (total outstanding, overdue, utilization)
```

#### 2.4.4 Business Rules
- BR-CR-01: Credit accounts require approval by a user with `CRM.Credit.Approve` permission before becoming active.
- BR-CR-02: Credit sales are rejected if the charge would exceed the customer's available credit (`AvailableCredit = CreditLimit - OutstandingBalance`).
- BR-CR-03: A credit sale at POS creates a `CreditTransaction` of type `Charge` with a due date of `TransactionDate + PaymentTermDays`.
- BR-CR-04: When a customer payment is recorded, it is applied to the oldest outstanding charges first (FIFO).
- BR-CR-05: If any charge is overdue by more than the configured grace period (default: 7 days), the credit account is automatically suspended.
- BR-CR-06: Suspended credit accounts cannot be used for new credit sales at POS.
- BR-CR-07: Credit limit changes require approval if increasing by more than 20% of the current limit.
- BR-CR-08: Write-offs require `Finance.WriteOff` permission and generate a corresponding journal entry (Debit: Bad Debt Expense, Credit: Accounts Receivable).
- BR-CR-09: The overdue report includes: customer name, days overdue, amount overdue, last payment date.
- BR-CR-10: Credit transactions generate corresponding entries in the Finance module's Accounts Receivable.

#### 2.4.5 Acceptance Criteria
- AC-CR-01: A newly created credit account is in `PendingApproval` status and cannot be used until approved.
- AC-CR-02: A POS credit sale for 200 GEL reduces available credit by 200 GEL and increases outstanding balance.
- AC-CR-03: Recording a 150 GEL payment reduces the outstanding balance and increases available credit.
- AC-CR-04: Attempting a credit sale that exceeds available credit is rejected with a clear error message.
- AC-CR-05: The overdue background job correctly identifies charges past their due date and suspends accounts.
- AC-CR-06: Write-off of 500 GEL creates a corresponding Bad Debt journal entry in the Finance module.

---

### 2.5 Purchase Analytics

#### 2.5.1 Description
Provide analytical insights into customer purchase behavior, including product preferences, category affinity, purchase frequency, recency-frequency-monetary (RFM) scoring, and churn prediction.

#### 2.5.2 New Entities

```csharp
// Domain/CRM/CustomerAnalytics.cs
public class CustomerAnalytics : BaseEntity
{
    public Guid CustomerId { get; private set; }

    // RFM Scores
    public int RecencyScore { get; private set; }               // 1-5 (5 = most recent)
    public int FrequencyScore { get; private set; }             // 1-5 (5 = most frequent)
    public int MonetaryScore { get; private set; }              // 1-5 (5 = highest spend)
    public string RfmSegment { get; private set; } = default!;  // "Champions", "At Risk", etc.

    // Behavioral Metrics
    public decimal AverageTransactionValue { get; private set; }
    public decimal AverageItemsPerTransaction { get; private set; }
    public int DaysBetweenVisits { get; private set; }
    public string? PreferredStore { get; private set; }
    public string? PreferredPaymentMethod { get; private set; }
    public string? TopCategoriesJson { get; private set; }       // JSON array of top 5 categories
    public string? TopProductsJson { get; private set; }         // JSON array of top 10 products

    // Churn Indicators
    public decimal ChurnProbability { get; private set; }        // 0.0 - 1.0
    public bool IsAtRisk { get; private set; }
    public int DaysSinceLastVisit { get; private set; }

    // Lifecycle
    public DateTimeOffset FirstPurchaseAt { get; private set; }
    public int CustomerLifetimeDays { get; private set; }
    public decimal CustomerLifetimeValue { get; private set; }

    public DateTimeOffset CalculatedAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
}
```

#### 2.5.3 API Endpoints

```
GET    /api/v1/crm/customers/{customerId}/analytics
       -> Customer analytics detail

GET    /api/v1/crm/analytics/rfm-distribution
       -> RFM segment distribution across all customers

GET    /api/v1/crm/analytics/top-customers
       ?metric={TotalPurchases|Visits|AvgTransaction|LifetimeValue}
       &limit={int}
       &period={ThisMonth|ThisQuarter|ThisYear|AllTime}
       -> Top N customers by metric

GET    /api/v1/crm/analytics/churn-risk
       ?minProbability={decimal}
       -> Customers at risk of churning

GET    /api/v1/crm/analytics/cohort
       ?cohortPeriod={Monthly|Quarterly}
       &metric={RetentionRate|AvgSpend|Visits}
       -> Cohort analysis

GET    /api/v1/crm/customers/{customerId}/purchase-history
       ?from={ISO8601}&to={ISO8601}
       &groupBy={Category|Product|Month}
       -> Detailed purchase breakdown

POST   /api/v1/crm/analytics/recalculate
       -> Trigger full analytics recalculation (background job)
```

#### 2.5.4 RFM Segment Definitions

| Segment | R | F | M | Description |
|---------|---|---|---|-------------|
| Champions | 5 | 5 | 5 | Best customers, buy often, spend a lot |
| Loyal Customers | 3-4 | 4-5 | 4-5 | Regular buyers, good spend |
| Potential Loyalists | 4-5 | 2-3 | 2-3 | Recent customers with moderate frequency |
| New Customers | 5 | 1 | 1-2 | Very recent first-time or second-time buyers |
| At Risk | 1-2 | 3-5 | 3-5 | Used to buy frequently but haven't recently |
| Hibernating | 1-2 | 1-2 | 1-2 | Low activity across all dimensions |
| Lost | 1 | 1-2 | 3-5 | Were good customers, now gone |

#### 2.5.5 Business Rules
- BR-AN-01: RFM scores are calculated on a 1-5 scale using quintile-based distribution across the active customer base.
- BR-AN-02: Analytics are recalculated by a background job nightly (02:00 UTC+4).
- BR-AN-03: Churn probability is calculated based on: days since last visit vs. average visit interval, declining transaction values, and reduced visit frequency.
- BR-AN-04: Top categories and products are based on purchase value, not quantity.
- BR-AN-05: Customer Lifetime Value (CLV) = Average Monthly Spend * Expected Customer Lifetime Months (based on retention rate).
- BR-AN-06: The "At Risk" flag is set when churn probability exceeds 0.7 or DaysSinceLastVisit exceeds 2x the customer's average visit interval.
- BR-AN-07: Cohort analysis groups customers by their first purchase month and tracks retention over subsequent months.
- BR-AN-08: Analytics calculations exclude voided transactions.

#### 2.5.6 Acceptance Criteria
- AC-AN-01: RFM scores correctly quintile-rank customers (each score group contains roughly 20% of customers).
- AC-AN-02: Top 10 products for a customer match a manual count of their purchase history.
- AC-AN-03: Churn probability increases when a previously regular customer stops visiting.
- AC-AN-04: Cohort analysis shows correct month-over-month retention rates.
- AC-AN-05: Analytics recalculation for 50,000 customers completes within 10 minutes.
- AC-AN-06: CLV calculation produces reasonable values consistent with actual purchase data.

---

### 2.6 Customer Address Management

#### 2.6.1 Description
Structured address storage for customers with support for multiple addresses (billing, delivery), Georgian address format, and address validation.

#### 2.6.2 New Entity

```csharp
// Domain/CRM/CustomerAddress.cs
public class CustomerAddress : BaseEntity
{
    public Guid CustomerId { get; private set; }
    public AddressType AddressType { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }                    // Tbilisi, Batumi, Kutaisi, etc.
    public string? Region { get; private set; }                  // Tbilisi, Adjara, Imereti, etc.
    public string? PostalCode { get; private set; }
    public string Country { get; private set; } = "GE";
    public bool IsDefault { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public Customer Customer { get; private set; } = default!;
}

public enum AddressType
{
    Billing,
    Delivery,
    Registered,         // Legal/registered address for B2B customers
    Other
}
```

#### 2.6.3 API Endpoints

```
GET    /api/v1/crm/customers/{customerId}/addresses
POST   /api/v1/crm/customers/{customerId}/addresses
PUT    /api/v1/crm/customers/{customerId}/addresses/{addressId}
DELETE /api/v1/crm/customers/{customerId}/addresses/{addressId}
POST   /api/v1/crm/customers/{customerId}/addresses/{addressId}/set-default
```

#### 2.6.4 Business Rules
- BR-AD-01: Each customer can have multiple addresses but only one default address per type.
- BR-AD-02: Setting a new default address of the same type unsets the previous default.
- BR-AD-03: Georgian postal codes follow the format NNNN (4 digits).
- BR-AD-04: City values are validated against a predefined list of Georgian municipalities.
- BR-AD-05: B2B customers (those with a TIN) should have a Registered address.

#### 2.6.5 Acceptance Criteria
- AC-AD-01: Creating two billing addresses and setting the second as default unsets the first.
- AC-AD-02: Georgian postal code format is validated (4 digits).
- AC-AD-03: Deleting the default address makes no address the default (user must set a new one).

---

## 3. Modifications to Existing Customer Entity

### 3.1 New Navigation Properties

Add to `Customer.cs`:
```csharp
public ICollection<CustomerAddress> Addresses { get; private set; }
    = new List<CustomerAddress>();
public ICollection<Communication> Communications { get; private set; }
    = new List<Communication>();
public ICollection<CustomerNote> Notes { get; private set; }
    = new List<CustomerNote>();
public CustomerCredit? Credit { get; private set; }
public CustomerAnalytics? Analytics { get; private set; }
public ICollection<CustomerSegmentMember> SegmentMemberships { get; private set; }
    = new List<CustomerSegmentMember>();
```

---

## 4. Required Permissions

| Permission | Description |
|------------|-------------|
| `CRM.Segment.View` | View customer segments |
| `CRM.Segment.Manage` | Create, update, delete segments |
| `CRM.Loyalty.View` | View loyalty program configuration |
| `CRM.Loyalty.Manage` | Configure loyalty programs, tiers, and rules |
| `CRM.Loyalty.Adjust` | Manually adjust customer loyalty points |
| `CRM.Communication.View` | View communication history |
| `CRM.Communication.Send` | Send individual communications |
| `CRM.Campaign.View` | View campaigns |
| `CRM.Campaign.Manage` | Create and execute campaigns |
| `CRM.Credit.View` | View credit accounts |
| `CRM.Credit.Manage` | Create and update credit accounts |
| `CRM.Credit.Approve` | Approve credit accounts and limit increases |
| `CRM.Analytics.View` | View customer analytics and reports |
| `CRM.Notes.Manage` | Create, edit, delete customer notes |

---

## 5. Background Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `SegmentEvaluationJob` | Daily at 03:00 UTC+4 | Re-evaluate all dynamic segments |
| `LoyaltyPointExpiryJob` | Daily at 01:00 UTC+4 | Expire points past their expiration date |
| `LoyaltyExpiryNotificationJob` | Daily at 10:00 UTC+4 | Notify customers with points expiring within 30 days |
| `CustomerAnalyticsJob` | Daily at 02:00 UTC+4 | Recalculate RFM scores, churn probability, CLV |
| `CreditOverdueCheckJob` | Daily at 08:00 UTC+4 | Check for overdue credit charges and suspend accounts |
| `CampaignProcessorJob` | Continuous (queue-based) | Process queued campaign messages |

---

## 6. Database Migration Considerations

### 6.1 New Tables
- `customer_segments` -- segment definitions
- `customer_segment_members` -- segment membership (junction table)
- `loyalty_programs` -- loyalty program configuration
- `loyalty_tiers` -- tier definitions
- `loyalty_earn_rules` -- point earning rules
- `loyalty_point_expiries` -- point expiration tracking
- `communications` -- communication history
- `campaigns` -- marketing campaign definitions
- `customer_notes` -- free-form notes
- `customer_credits` -- credit account management
- `credit_transactions` -- credit transaction ledger
- `customer_analytics` -- pre-calculated analytics (materialized)
- `customer_addresses` -- structured addresses

### 6.2 Indexes
- `idx_segment_members_customer` on `customer_segment_members(customer_id)` for fast segment lookup
- `idx_communications_customer_date` on `communications(customer_id, created_at DESC)` for history queries
- `idx_credit_transactions_credit` on `credit_transactions(customer_credit_id, created_at)` for ledger queries
- `idx_loyalty_expiry_customer` on `loyalty_point_expiries(customer_id, expires_at)` for expiration processing
- `idx_analytics_rfm` on `customer_analytics(rfm_segment)` for segment distribution queries

### 6.3 Seed Data
- Default loyalty program with 4 tiers (Bronze/Silver/Gold/Platinum)
- Pre-defined customer segments: "VIP", "At Risk", "New Customers", "Inactive"
- Georgian city/region reference data for address validation

---

## 7. Integration Points

### 7.1 POS Module
- Loyalty point calculation during sale
- Credit sale authorization
- Customer identification (loyalty card scan, phone lookup)

### 7.2 Finance Module
- Credit transactions create corresponding AR entries
- Write-offs generate journal entries
- Credit payments reconcile against AR

### 7.3 Notification Module
- Loyalty point expiry warnings
- Credit payment reminders
- Campaign message delivery

### 7.4 Pricing Module
- Tier-based automatic discounts
- Segment-targeted promotions

---

## 8. Testing Strategy

### 8.1 Unit Tests
- Segment rule evaluation engine with various condition combinations
- Loyalty point calculation with tier multipliers and earn rules
- RFM score calculation with known data sets
- Credit limit and available credit calculations
- FIFO point expiration logic

### 8.2 Integration Tests
- End-to-end: POS sale -> loyalty point earning -> tier upgrade
- End-to-end: Credit sale -> AR entry -> payment -> balance update
- Campaign send -> message queue -> delivery tracking
- Dynamic segment evaluation with database queries

### 8.3 Performance Tests
- Segment evaluation for 100,000 customers: < 60 seconds
- Loyalty calculation at POS: < 200ms (p99)
- Analytics recalculation for 50,000 customers: < 10 minutes
- Campaign send for 10,000 recipients: queued within 30 seconds
