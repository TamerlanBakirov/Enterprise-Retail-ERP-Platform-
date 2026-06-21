# Approval Workflow Engine Specification

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 21, 2026
**Status:** Draft

---

## 1. Overview

### 1.1 Objective
Design a generic, configurable approval engine that can be applied to any business document requiring authorization before execution. The engine must support multi-level approvals, delegation, escalation, and integration with the notification system.

### 1.2 Supported Document Types (Initial)

| Document Type | Entity | Current State |
|---------------|--------|---------------|
| Purchase Orders | `PurchaseOrder` | Has `PendingApproval` status and `Approve()` method but no workflow engine |
| Stock Transfers | `TransferOrder` | Has `PendingApproval` status and `Approve()` method but no workflow engine |
| Price Changes | `PriceList` | No approval mechanism |
| Refunds/Voids | `PosTransaction` (Void) | No approval mechanism |

### 1.3 Design Principles
- **Generic**: The engine is document-type agnostic. New document types can be added via configuration.
- **Configurable**: Approval chains, thresholds, and escalation rules are configurable without code changes.
- **Auditable**: Every approval action is recorded with timestamp, user, and reason.
- **Extensible**: Notification hooks allow integration with the notification system.

---

## 2. Domain Model

### 2.1 Core Entities

```csharp
// Domain/Workflow/ApprovalWorkflowDefinition.cs
public class ApprovalWorkflowDefinition : BaseEntity
{
    public string Code { get; private set; } = default!;
    public string Name { get; private set; } = default!;
    public string? NameKa { get; private set; }
    public string DocumentType { get; private set; } = default!;    // "PurchaseOrder", "TransferOrder", etc.
    public string? Description { get; private set; }
    public bool IsActive { get; private set; }
    public int Version { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public ICollection<ApprovalStep> Steps { get; private set; }
        = new List<ApprovalStep>();
    public ICollection<ApprovalThresholdRule> ThresholdRules { get; private set; }
        = new List<ApprovalThresholdRule>();
}

// Domain/Workflow/ApprovalStep.cs
public class ApprovalStep : BaseEntity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public int StepOrder { get; private set; }                      // 1, 2, 3...
    public string Name { get; private set; } = default!;            // "Manager Approval", "Director Approval"
    public string? NameKa { get; private set; }
    public ApprovalStepType StepType { get; private set; }
    public ApproverSelectionMode ApproverMode { get; private set; }
    public Guid? ApproverUserId { get; private set; }               // For SpecificUser mode
    public Guid? ApproverRoleId { get; private set; }               // For Role mode
    public string? ApproverExpression { get; private set; }         // For Dynamic mode (e.g., "document.Store.ManagerId")
    public bool AllApproversRequired { get; private set; }          // true = all must approve, false = any one
    public int EscalationHours { get; private set; }                // Hours before auto-escalation (0 = no escalation)
    public Guid? EscalateToUserId { get; private set; }             // Who to escalate to
    public bool AutoApproveOnEscalation { get; private set; }       // Auto-approve if escalation timeout reached
    public DateTimeOffset CreatedAt { get; private set; }

    public ApprovalWorkflowDefinition WorkflowDefinition { get; private set; } = default!;
}

public enum ApprovalStepType
{
    Approval,           // Standard approval step
    Review,             // Review only (informational, no blocking)
    Notification        // Send notification only, auto-advance
}

public enum ApproverSelectionMode
{
    SpecificUser,       // A named user
    Role,               // Any user with a specific role
    Dynamic,            // Determined at runtime (e.g., store manager)
    Requester           // The person who submitted (for self-approval under threshold)
}

// Domain/Workflow/ApprovalThresholdRule.cs
public class ApprovalThresholdRule : BaseEntity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public string Field { get; private set; } = default!;           // "Total", "Quantity", "LineCount"
    public ThresholdOperator Operator { get; private set; }
    public decimal ThresholdValue { get; private set; }
    public int RequiredStepCount { get; private set; }              // How many steps are required when threshold met
    public Guid? OverrideStepId { get; private set; }               // Jump to a specific step
    public bool SkipAllSteps { get; private set; }                  // Auto-approve below threshold
    public DateTimeOffset CreatedAt { get; private set; }

    public ApprovalWorkflowDefinition WorkflowDefinition { get; private set; } = default!;
}

public enum ThresholdOperator
{
    LessThan,
    LessThanOrEqual,
    GreaterThan,
    GreaterThanOrEqual,
    Between
}

// Domain/Workflow/ApprovalRequest.cs
public class ApprovalRequest : BaseEntity
{
    public Guid WorkflowDefinitionId { get; private set; }
    public string DocumentType { get; private set; } = default!;
    public Guid DocumentId { get; private set; }
    public string DocumentNumber { get; private set; } = default!;  // PO-2026-001, TR-2026-001, etc.
    public string? DocumentSummary { get; private set; }            // Human-readable summary
    public decimal? DocumentAmount { get; private set; }            // For threshold evaluation
    public int CurrentStepOrder { get; private set; }
    public ApprovalRequestStatus Status { get; private set; }
    public Guid RequestedBy { get; private set; }
    public DateTimeOffset RequestedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }
    public string? CompletionNotes { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public ApprovalWorkflowDefinition WorkflowDefinition { get; private set; } = default!;
    public ICollection<ApprovalAction> Actions { get; private set; }
        = new List<ApprovalAction>();
}

public enum ApprovalRequestStatus
{
    Pending,            // Awaiting approval at current step
    Approved,           // All steps approved
    Rejected,           // Rejected at any step
    Cancelled,          // Cancelled by requester
    Escalated,          // Current step has been escalated
    Expired             // Timed out without action
}

// Domain/Workflow/ApprovalAction.cs
public class ApprovalAction : BaseEntity
{
    public Guid ApprovalRequestId { get; private set; }
    public int StepOrder { get; private set; }
    public Guid ActionBy { get; private set; }
    public ApprovalActionType ActionType { get; private set; }
    public string? Comments { get; private set; }
    public string? CommentsKa { get; private set; }
    public DateTimeOffset ActionAt { get; private set; }
    public string? DelegatedFrom { get; private set; }              // Original approver if delegated

    public ApprovalRequest ApprovalRequest { get; private set; } = default!;
}

public enum ApprovalActionType
{
    Approved,
    Rejected,
    Returned,           // Sent back for revision
    Escalated,          // Manually escalated
    AutoEscalated,      // System escalated due to timeout
    Delegated,          // Delegated to another user
    Commented           // Comment added without approval decision
}

// Domain/Workflow/ApprovalDelegation.cs
public class ApprovalDelegation : BaseEntity
{
    public Guid DelegatorUserId { get; private set; }
    public Guid DelegateUserId { get; private set; }
    public DateTimeOffset ValidFrom { get; private set; }
    public DateTimeOffset ValidTo { get; private set; }
    public string? DocumentType { get; private set; }               // null = all document types
    public string? Reason { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

---

## 3. State Machine

### 3.1 Approval Request Lifecycle

```
                    +-----------+
                    |  Created  |
                    +-----+-----+
                          |
                   Submit for Approval
                          |
                    +-----v-----+
              +---->|  Pending   |<--------+
              |     +-----+-----+         |
              |           |               |
              |     +-----+------+        |
              |     |     |      |        |
              |  Approve Reject Return    |
              |     |     |      |        |
              |     v     v      v        |
              |  +--+-+ +--+-+ +-+-+      |
              |  |Next| |Rej | |Rev|      |
              |  |Step| |ect | |ise|------+
              |  +--+-+ +----+ +---+  (resubmit)
              |     |
              |     v
              |  All Steps Done?
              |     |
              |  +--v----+
              |  |Approved|
              |  +--------+
              |
         Escalation
         (timeout)
              |
         +----v------+
         | Escalated  |
         +-----+------+
               |
          Escalation
          Approver
          Decides
               |
         +-----v------+
         | Approved    |
         | or Rejected |
         +-------------+
```

### 3.2 State Transitions

| Current State | Action | Next State | Conditions |
|---------------|--------|------------|------------|
| (none) | Submit | Pending | Document passes validation |
| Pending | Approve | Pending (next step) | Current step approved; more steps exist |
| Pending | Approve | Approved | Current step approved; last step |
| Pending | Reject | Rejected | Any step can reject |
| Pending | Return | Pending (step 1) | Returned for revision; requester must resubmit |
| Pending | Escalate | Escalated | Manual or auto-escalation |
| Pending | Cancel | Cancelled | Only by requester |
| Escalated | Approve | Approved / Pending (next) | Escalation approver decides |
| Escalated | Reject | Rejected | Escalation approver decides |
| Approved | (none) | Terminal | Triggers document-specific action |
| Rejected | (none) | Terminal | Requester may create new request |
| Cancelled | (none) | Terminal | No further action |

### 3.3 Document Actions on Approval

| Document Type | Action on Approved | Action on Rejected |
|---------------|-------------------|-------------------|
| PurchaseOrder | `PurchaseOrder.Approve(approvedBy)` -> status becomes `Approved` | Status remains `Draft` |
| TransferOrder | `TransferOrder.Approve(approvedBy)` -> status becomes `Approved` | Status remains `Draft` |
| PriceList | Activate the price list (set `IsActive = true`) | Price list stays inactive |
| PosTransaction (Void) | Execute `PosTransaction.Void(voidedBy, reason)` | Void is denied; transaction unchanged |

---

## 4. API Endpoints

### 4.1 Workflow Definition Management

```
GET    /api/v1/workflows/definitions
       ?documentType={string}
       &isActive={bool}
       -> List workflow definitions

GET    /api/v1/workflows/definitions/{id}
       -> Workflow definition with steps and threshold rules

POST   /api/v1/workflows/definitions
       Body: {
           code, name, nameKa, documentType, description,
           steps: [{ stepOrder, name, nameKa, stepType, approverMode, ... }],
           thresholdRules: [{ field, operator, thresholdValue, ... }]
       }
       -> Create workflow definition

PUT    /api/v1/workflows/definitions/{id}
       -> Update workflow definition (creates new version)

POST   /api/v1/workflows/definitions/{id}/activate
POST   /api/v1/workflows/definitions/{id}/deactivate
```

### 4.2 Approval Request Operations

```
POST   /api/v1/workflows/submit
       Body: { documentType, documentId, documentNumber, documentSummary, documentAmount }
       -> Submit a document for approval (creates ApprovalRequest)

GET    /api/v1/workflows/requests
       ?status={Pending|Approved|Rejected|Cancelled}
       &documentType={string}
       &requestedBy={guid}
       &page={int}&pageSize={int}
       -> List approval requests

GET    /api/v1/workflows/requests/{id}
       -> Approval request detail with action history

POST   /api/v1/workflows/requests/{id}/approve
       Body: { comments, commentsKa }
       -> Approve current step

POST   /api/v1/workflows/requests/{id}/reject
       Body: { comments, commentsKa }
       -> Reject the request

POST   /api/v1/workflows/requests/{id}/return
       Body: { comments, commentsKa }
       -> Return for revision

POST   /api/v1/workflows/requests/{id}/escalate
       Body: { escalateToUserId, reason }
       -> Manually escalate

POST   /api/v1/workflows/requests/{id}/cancel
       -> Cancel (by requester only)

GET    /api/v1/workflows/my-pending
       -> List requests awaiting the current user's approval

GET    /api/v1/workflows/my-requests
       -> List requests submitted by the current user
```

### 4.3 Delegation Management

```
GET    /api/v1/workflows/delegations
       -> List active delegations for current user

POST   /api/v1/workflows/delegations
       Body: { delegateUserId, validFrom, validTo, documentType, reason }
       -> Create a delegation

DELETE /api/v1/workflows/delegations/{id}
       -> Revoke a delegation
```

---

## 5. Workflow Configuration Examples

### 5.1 Purchase Order Workflow

```json
{
    "code": "PO-APPROVAL",
    "name": "Purchase Order Approval",
    "nameKa": "შესყიდვის ორდერის დამტკიცება",
    "documentType": "PurchaseOrder",
    "steps": [
        {
            "stepOrder": 1,
            "name": "Store Manager Approval",
            "stepType": "Approval",
            "approverMode": "Dynamic",
            "approverExpression": "document.Warehouse.Store.ManagerId",
            "escalationHours": 24,
            "autoApproveOnEscalation": false
        },
        {
            "stepOrder": 2,
            "name": "Finance Director Approval",
            "stepType": "Approval",
            "approverMode": "Role",
            "approverRoleId": "FINANCE_DIRECTOR",
            "allApproversRequired": false,
            "escalationHours": 48
        }
    ],
    "thresholdRules": [
        {
            "field": "Total",
            "operator": "LessThanOrEqual",
            "thresholdValue": 500,
            "skipAllSteps": true
        },
        {
            "field": "Total",
            "operator": "LessThanOrEqual",
            "thresholdValue": 5000,
            "requiredStepCount": 1
        },
        {
            "field": "Total",
            "operator": "GreaterThan",
            "thresholdValue": 5000,
            "requiredStepCount": 2
        }
    ]
}
```

**Result:**
- POs <= 500 GEL: Auto-approved
- POs 501-5,000 GEL: Store Manager approval only
- POs > 5,000 GEL: Store Manager + Finance Director approval

### 5.2 Void/Refund Workflow

```json
{
    "code": "VOID-APPROVAL",
    "name": "Void/Refund Approval",
    "documentType": "PosTransactionVoid",
    "steps": [
        {
            "stepOrder": 1,
            "name": "Shift Supervisor Approval",
            "stepType": "Approval",
            "approverMode": "Role",
            "approverRoleId": "SHIFT_SUPERVISOR",
            "escalationHours": 1
        },
        {
            "stepOrder": 2,
            "name": "Store Manager Approval",
            "stepType": "Approval",
            "approverMode": "Dynamic",
            "approverExpression": "document.Session.Terminal.Store.ManagerId",
            "escalationHours": 4
        }
    ],
    "thresholdRules": [
        {
            "field": "Total",
            "operator": "LessThanOrEqual",
            "thresholdValue": 50,
            "requiredStepCount": 1
        },
        {
            "field": "Total",
            "operator": "GreaterThan",
            "thresholdValue": 50,
            "requiredStepCount": 2
        }
    ]
}
```

### 5.3 Stock Transfer Workflow

```json
{
    "code": "TRANSFER-APPROVAL",
    "name": "Stock Transfer Approval",
    "documentType": "TransferOrder",
    "steps": [
        {
            "stepOrder": 1,
            "name": "Source Warehouse Manager",
            "stepType": "Approval",
            "approverMode": "Dynamic",
            "approverExpression": "document.SourceWarehouse.ManagerId",
            "escalationHours": 24
        }
    ],
    "thresholdRules": []
}
```

### 5.4 Price Change Workflow

```json
{
    "code": "PRICE-CHANGE-APPROVAL",
    "name": "Price Change Approval",
    "documentType": "PriceList",
    "steps": [
        {
            "stepOrder": 1,
            "name": "Category Manager Review",
            "stepType": "Review",
            "approverMode": "Role",
            "approverRoleId": "CATEGORY_MANAGER",
            "escalationHours": 48
        },
        {
            "stepOrder": 2,
            "name": "Commercial Director Approval",
            "stepType": "Approval",
            "approverMode": "Role",
            "approverRoleId": "COMMERCIAL_DIRECTOR",
            "escalationHours": 72
        }
    ],
    "thresholdRules": []
}
```

---

## 6. Notification Hooks

The approval engine raises domain events that the Notification System consumes:

### 6.1 Domain Events

```csharp
// Domain/Workflow/Events/ApprovalRequestedEvent.cs
public record ApprovalRequestedEvent : DomainEvent
{
    public Guid ApprovalRequestId { get; init; }
    public string DocumentType { get; init; } = default!;
    public string DocumentNumber { get; init; } = default!;
    public Guid RequestedBy { get; init; }
    public Guid CurrentApprover { get; init; }              // or list for Role-based
    public int StepOrder { get; init; }
    public string StepName { get; init; } = default!;
}

// Domain/Workflow/Events/ApprovalDecisionEvent.cs
public record ApprovalDecisionEvent : DomainEvent
{
    public Guid ApprovalRequestId { get; init; }
    public string DocumentType { get; init; } = default!;
    public string DocumentNumber { get; init; } = default!;
    public Guid DecisionBy { get; init; }
    public ApprovalActionType Decision { get; init; }
    public string? Comments { get; init; }
    public Guid RequestedBy { get; init; }                  // Notify requester
}

// Domain/Workflow/Events/ApprovalEscalatedEvent.cs
public record ApprovalEscalatedEvent : DomainEvent
{
    public Guid ApprovalRequestId { get; init; }
    public string DocumentType { get; init; } = default!;
    public string DocumentNumber { get; init; } = default!;
    public Guid EscalatedTo { get; init; }
    public Guid OriginalApprover { get; init; }
    public int HoursOverdue { get; init; }
}

// Domain/Workflow/Events/ApprovalCompletedEvent.cs
public record ApprovalCompletedEvent : DomainEvent
{
    public Guid ApprovalRequestId { get; init; }
    public string DocumentType { get; init; } = default!;
    public Guid DocumentId { get; init; }
    public string DocumentNumber { get; init; } = default!;
    public ApprovalRequestStatus FinalStatus { get; init; }
    public Guid RequestedBy { get; init; }
}
```

### 6.2 Notification Triggers

| Event | Recipient | Channel | Message |
|-------|-----------|---------|---------|
| ApprovalRequested | Current step approver(s) | InApp + Email | "PO-2026-001 requires your approval (500.00 GEL)" |
| ApprovalDecision (Approved) | Requester | InApp | "Your PO-2026-001 has been approved by [name]" |
| ApprovalDecision (Rejected) | Requester | InApp + Email | "Your PO-2026-001 has been rejected: [reason]" |
| ApprovalEscalated | Escalation approver | InApp + Email + SMS | "PO-2026-001 has been escalated to you (24h overdue)" |
| ApprovalCompleted (Approved) | Requester | InApp | "PO-2026-001 has been fully approved" |
| ApprovalCompleted (Rejected) | Requester | InApp + Email | "PO-2026-001 has been rejected" |

---

## 7. Escalation Rules

### 7.1 Automatic Escalation

```csharp
// Application/Workflow/Jobs/ApprovalEscalationJob.cs
// Runs every 15 minutes
```

**Logic:**
1. Find all `Pending` approval requests where `CurrentStep.EscalationHours > 0`.
2. For each, check if `Now - LastActionAt > EscalationHours`.
3. If overdue:
   a. If `EscalateToUserId` is set, reassign to that user.
   b. If `AutoApproveOnEscalation` is true, auto-approve the step.
   c. Log an `AutoEscalated` action.
   d. Raise `ApprovalEscalatedEvent`.

### 7.2 Reminder Notifications

| Timing | Action |
|--------|--------|
| 50% of escalation time | Send reminder to current approver |
| 75% of escalation time | Send urgent reminder to current approver |
| 100% of escalation time | Escalate (see above) |

---

## 8. Business Rules

| Rule | Description |
|------|-------------|
| BR-WF-01 | Only one active workflow definition per document type at a time. |
| BR-WF-02 | A document can have only one active (Pending/Escalated) approval request at a time. |
| BR-WF-03 | The requester cannot approve their own request (unless step is configured with `ApproverMode = Requester`). |
| BR-WF-04 | Threshold rules are evaluated at submission time. The number of required steps is determined by the highest-matching threshold. |
| BR-WF-05 | If no workflow definition exists for a document type, the document action proceeds without approval. |
| BR-WF-06 | Delegated approvals are logged with the delegate's ID and a reference to the original approver. |
| BR-WF-07 | Delegation periods must not overlap for the same delegator. |
| BR-WF-08 | Rejected requests can be resubmitted (creates a new ApprovalRequest linked to the same document). |
| BR-WF-09 | Returned requests reset to step 1 and increment a revision counter. |
| BR-WF-10 | All approval actions are immutable (append-only audit trail). |
| BR-WF-11 | Updating a workflow definition creates a new version; existing in-flight requests continue using the version they started with. |
| BR-WF-12 | Review-type steps auto-advance after 24 hours if no action is taken. |

---

## 9. Required Permissions

| Permission | Description |
|------------|-------------|
| `Workflow.Definition.View` | View workflow definitions |
| `Workflow.Definition.Manage` | Create, update, activate/deactivate workflow definitions |
| `Workflow.Request.View` | View approval requests |
| `Workflow.Request.Submit` | Submit documents for approval |
| `Workflow.Request.Approve` | Approve/reject approval requests (scoped by document type) |
| `Workflow.Request.Escalate` | Manually escalate a request |
| `Workflow.Delegation.Manage` | Create and revoke delegations |

---

## 10. Database Tables

### 10.1 New Tables
- `approval_workflow_definitions` -- workflow templates
- `approval_steps` -- step definitions within a workflow
- `approval_threshold_rules` -- amount-based routing rules
- `approval_requests` -- active/completed approval instances
- `approval_actions` -- action audit trail (append-only)
- `approval_delegations` -- temporary delegation records

### 10.2 Indexes
- `idx_approval_requests_document` on `approval_requests(document_type, document_id)` -- find requests for a document
- `idx_approval_requests_status` on `approval_requests(status, current_step_order)` -- find pending requests
- `idx_approval_actions_request` on `approval_actions(approval_request_id, action_at)` -- action history
- `idx_approval_delegations_active` on `approval_delegations(delegator_user_id, valid_from, valid_to)` -- check active delegations

---

## 11. Integration with Existing Entities

### 11.1 Modifications Required

**PurchaseOrder** -- modify the `Approve()` method to check for workflow:
```csharp
public void SubmitForApproval()
{
    if (Status != PurchaseOrderStatus.Draft)
        throw new InvalidOperationException("Only draft POs can be submitted for approval.");
    Status = PurchaseOrderStatus.PendingApproval;
    Touch();
    // Raise ApprovalSubmittedEvent for the workflow engine to pick up
}
```

**TransferOrder** -- similar modification to `Approve()`.

**PriceList** -- add status tracking:
```csharp
public PriceListApprovalStatus ApprovalStatus { get; private set; }
public void SubmitForApproval() { ... }
```

**PosTransaction** -- void requests go through approval:
```csharp
public void RequestVoid(Guid requestedBy, string reason)
{
    // Creates an approval request; actual void happens on approval completion
}
```

### 11.2 Application Service Integration

```csharp
// Application/Workflow/Services/IApprovalService.cs
public interface IApprovalService
{
    Task<Result<Guid>> SubmitForApprovalAsync(
        string documentType,
        Guid documentId,
        string documentNumber,
        string? documentSummary,
        decimal? documentAmount,
        Guid requestedBy,
        CancellationToken ct);

    Task<Result> ApproveAsync(Guid approvalRequestId, Guid approvedBy, string? comments, CancellationToken ct);
    Task<Result> RejectAsync(Guid approvalRequestId, Guid rejectedBy, string? comments, CancellationToken ct);
    Task<Result> ReturnAsync(Guid approvalRequestId, Guid returnedBy, string? comments, CancellationToken ct);
    Task<bool> RequiresApprovalAsync(string documentType, decimal? amount, CancellationToken ct);
}
```

The `ApprovalCompletedEvent` handler calls the appropriate document-specific handler:
```csharp
// Application/Workflow/Handlers/ApprovalCompletedHandler.cs
public class ApprovalCompletedHandler : INotificationHandler<ApprovalCompletedEvent>
{
    // Resolves the document type and calls the corresponding action:
    // "PurchaseOrder" -> PurchaseOrder.Approve()
    // "TransferOrder" -> TransferOrder.Approve()
    // "PriceList" -> PriceList.Activate()
    // "PosTransactionVoid" -> PosTransaction.Void()
}
```

---

## 12. Background Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `ApprovalEscalationJob` | Every 15 minutes | Check for overdue approvals and escalate |
| `ApprovalReminderJob` | Every hour | Send reminders for pending approvals approaching escalation |
| `ApprovalExpirationJob` | Daily at 00:00 UTC+4 | Expire approval requests older than 30 days |

---

## 13. Testing Strategy

### 13.1 Unit Tests
- State machine transitions: each valid and invalid transition
- Threshold rule evaluation with various amounts
- Escalation time calculation
- Delegation overlap validation
- Approver resolution for each ApproverSelectionMode

### 13.2 Integration Tests
- End-to-end: PO creation -> submit -> manager approval -> finance approval -> PO status changes to Approved
- End-to-end: Void request -> supervisor approval -> void executed
- Threshold routing: PO under 500 GEL auto-approves, PO over 5,000 GEL requires 2 steps
- Escalation: pending request auto-escalates after configured timeout
- Delegation: delegated user can approve on behalf of the original approver
- Rejection: rejected PO stays in Draft status

### 13.3 Performance Tests
- Submitting 100 approval requests concurrently: all created without conflicts
- Escalation job processing 1,000 pending requests: completes within 60 seconds

---

## 14. Acceptance Criteria (End-to-End)

- AC-WF-01: A Purchase Order for 3,000 GEL is submitted and only requires Store Manager approval (1 step per threshold).
- AC-WF-02: A Purchase Order for 10,000 GEL requires both Store Manager and Finance Director approval (2 steps).
- AC-WF-03: A Purchase Order for 200 GEL is auto-approved immediately.
- AC-WF-04: Rejecting a PO at any step notifies the requester and leaves the PO in Draft status.
- AC-WF-05: After 24 hours without action, the pending request escalates and the escalation approver receives notification.
- AC-WF-06: A POS void request for 30 GEL requires only Shift Supervisor approval.
- AC-WF-07: A delegated user can approve on behalf of the original approver, and the action is recorded as delegated.
- AC-WF-08: The approval history shows all actions with timestamps, users, and comments.
- AC-WF-09: Updating a workflow definition does not affect in-flight approval requests.
- AC-WF-10: A returned request resets to step 1 and notifies the requester to revise.
