# Finance Module Completion Specification

## Enterprise Retail ERP Platform for Georgia

**Version:** 1.0
**Date:** June 21, 2026
**Status:** Draft
**Module Completion Target:** 78% -> 100%

---

## 1. Current State Assessment

### 1.1 Existing Entities

| Entity | File | Status |
|--------|------|--------|
| `ChartOfAccount` | `Domain/Finance/ChartOfAccount.cs` | Complete |
| `JournalEntry` | `Domain/Finance/JournalEntry.cs` | Complete |
| `JournalEntryLine` | `Domain/Finance/JournalEntryLine.cs` | Complete |
| `BankAccount` | `Domain/Finance/BankAccount.cs` | Complete |

### 1.2 Existing Application Handlers

| Handler | Status |
|---------|--------|
| `CreateAccountCommand` | Complete |
| `CreateBankAccountCommand` | Complete |
| `CreateJournalEntryCommand` | Complete (manual only) |
| `PostJournalEntryCommand` | Complete |
| `GetChartOfAccountsQuery` | Complete |
| `GetJournalEntriesQuery` | Complete (paginated) |
| `GetBankAccountsQuery` | Complete |
| FluentValidation validators | Complete for existing commands |

### 1.3 Gaps Identified

1. **No financial reports** -- Trial Balance, P&L, Balance Sheet
2. **No account reconciliation** -- bank statement matching absent
3. **No auto-journal generation** -- POS and Procurement entries are manual
4. **No multi-currency support** -- JournalEntryLine has no currency/rate fields
5. **No fiscal period management** -- no period close/lock mechanism
6. **No accounts receivable/payable** tracking entities
7. **No budget tracking**

---

## 2. Feature Specifications

### 2.1 Trial Balance Report

#### 2.1.1 Description
Generate a Trial Balance report showing all account balances (debit and credit totals) for a given date range, with optional filtering by account type and active status.

#### 2.1.2 New Entities
None required. The Trial Balance is a projection over existing `JournalEntryLine` and `ChartOfAccount` data.

#### 2.1.3 New DTOs

```csharp
public record TrialBalanceLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    string AccountType,
    decimal OpeningDebit,
    decimal OpeningCredit,
    decimal PeriodDebit,
    decimal PeriodCredit,
    decimal ClosingDebit,
    decimal ClosingCredit);

public record TrialBalanceReportDto(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    IReadOnlyList<TrialBalanceLineDto> Lines,
    decimal TotalDebit,
    decimal TotalCredit,
    bool IsBalanced);
```

#### 2.1.4 API Endpoint

```
GET /api/v1/finance/reports/trial-balance
    ?periodStart={ISO8601}
    &periodEnd={ISO8601}
    &accountType={Asset|Liability|Equity|Revenue|Expense}  (optional)
    &includeZeroBalances={bool}  (default: false)
```

**Response:** `TrialBalanceReportDto`

#### 2.1.5 Business Rules
- BR-TB-01: Only journal entries with status `Posted` are included.
- BR-TB-02: Opening balances are calculated from all posted entries before `periodStart`.
- BR-TB-03: Period debits/credits are summed from posted entries within `[periodStart, periodEnd]`.
- BR-TB-04: Closing balance = Opening + Period activity.
- BR-TB-05: `IsBalanced` is true when `TotalDebit == TotalCredit` (within 0.01 GEL tolerance for rounding).
- BR-TB-06: Header accounts (IsHeader = true) aggregate child account balances.
- BR-TB-07: Inactive accounts with non-zero balances are always included.

#### 2.1.6 Acceptance Criteria
- AC-TB-01: Given posted journal entries across multiple accounts, the Trial Balance report shows correct debit and credit totals that balance.
- AC-TB-02: Filtering by account type returns only accounts of that type.
- AC-TB-03: Zero-balance accounts are excluded by default but included when `includeZeroBalances=true`.
- AC-TB-04: Opening balances correctly reflect all activity before the period start.
- AC-TB-05: Report generation completes within 3 seconds for 500+ accounts and 10,000+ journal entries.

---

### 2.2 Profit & Loss Statement

#### 2.2.1 Description
Generate an Income Statement (P&L) showing Revenue minus Expenses for a given period, with comparative period support and drill-down to account detail.

#### 2.2.2 New DTOs

```csharp
public record PnlAccountLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    decimal CurrentPeriodAmount,
    decimal PriorPeriodAmount,
    decimal VarianceAmount,
    decimal VariancePercent);

public record PnlSectionDto(
    string SectionName,         // "Revenue", "Cost of Goods Sold", "Operating Expenses"
    IReadOnlyList<PnlAccountLineDto> Accounts,
    decimal SectionTotal,
    decimal PriorPeriodTotal);

public record ProfitAndLossReportDto(
    DateTimeOffset PeriodStart,
    DateTimeOffset PeriodEnd,
    DateTimeOffset? ComparativePeriodStart,
    DateTimeOffset? ComparativePeriodEnd,
    IReadOnlyList<PnlSectionDto> Sections,
    decimal GrossProfit,
    decimal OperatingProfit,
    decimal NetProfit,
    decimal PriorPeriodNetProfit,
    string Currency);
```

#### 2.2.3 API Endpoint

```
GET /api/v1/finance/reports/profit-and-loss
    ?periodStart={ISO8601}
    &periodEnd={ISO8601}
    &comparative={bool}           (default: false, prior same-length period)
    &storeId={guid}               (optional, filter by store)
    &currency={GEL|USD|EUR}       (default: GEL)
```

**Response:** `ProfitAndLossReportDto`

#### 2.2.4 Business Rules
- BR-PL-01: Revenue accounts (AccountType = Revenue) are shown as positive values.
- BR-PL-02: Expense accounts (AccountType = Expense) are shown as positive values (deducted from revenue).
- BR-PL-03: Gross Profit = Revenue - Cost of Goods Sold (COGS accounts identified by AccountCode prefix or a configurable mapping).
- BR-PL-04: Operating Profit = Gross Profit - Operating Expenses.
- BR-PL-05: Net Profit = Operating Profit - Other Expenses + Other Income.
- BR-PL-06: Comparative period defaults to the prior period of same duration (e.g., prior month).
- BR-PL-07: Variance % = ((Current - Prior) / |Prior|) * 100. If prior = 0, show null.
- BR-PL-08: Store-level P&L requires journal entries tagged with StoreId via the source reference.
- BR-PL-09: Multi-currency reports convert amounts using the exchange rate on the entry date (see Section 2.5).

#### 2.2.5 Acceptance Criteria
- AC-PL-01: Revenue and expense totals match the Trial Balance for the same period.
- AC-PL-02: Comparative period shows correct prior-period values and variance calculations.
- AC-PL-03: Store-level filtering produces a P&L scoped to that store's transactions.
- AC-PL-04: Net Profit = Total Revenue - Total Expenses (verified against journal totals).

---

### 2.3 Balance Sheet

#### 2.3.1 Description
Generate a Balance Sheet as of a specific date, showing Assets, Liabilities, and Equity, following the accounting equation: Assets = Liabilities + Equity.

#### 2.3.2 New DTOs

```csharp
public record BalanceSheetLineDto(
    Guid AccountId,
    string AccountCode,
    string AccountName,
    string? AccountNameKa,
    decimal Balance,
    decimal PriorPeriodBalance);

public record BalanceSheetSectionDto(
    string SectionName,             // "Current Assets", "Fixed Assets", "Current Liabilities", etc.
    IReadOnlyList<BalanceSheetLineDto> Accounts,
    decimal SectionTotal,
    decimal PriorSectionTotal);

public record BalanceSheetReportDto(
    DateTimeOffset AsOfDate,
    DateTimeOffset? ComparativeDate,
    IReadOnlyList<BalanceSheetSectionDto> AssetSections,
    IReadOnlyList<BalanceSheetSectionDto> LiabilitySections,
    IReadOnlyList<BalanceSheetSectionDto> EquitySections,
    decimal TotalAssets,
    decimal TotalLiabilities,
    decimal TotalEquity,
    decimal RetainedEarnings,
    bool IsBalanced,
    string Currency);
```

#### 2.3.3 API Endpoint

```
GET /api/v1/finance/reports/balance-sheet
    ?asOfDate={ISO8601}
    &comparative={bool}           (default: false)
    &comparativeDate={ISO8601}    (optional)
    &currency={GEL|USD|EUR}       (default: GEL)
```

**Response:** `BalanceSheetReportDto`

#### 2.3.4 Business Rules
- BR-BS-01: All posted journal entries up to and including `asOfDate` are included.
- BR-BS-02: Asset accounts show their net debit balance (Debit - Credit).
- BR-BS-03: Liability and Equity accounts show their net credit balance (Credit - Debit).
- BR-BS-04: Retained Earnings = Sum of all Revenue accounts - Sum of all Expense accounts (cumulative to date).
- BR-BS-05: The accounting equation must hold: TotalAssets = TotalLiabilities + TotalEquity + RetainedEarnings.
- BR-BS-06: `IsBalanced` is true when the equation holds within 0.01 GEL tolerance.
- BR-BS-07: Accounts are grouped into sub-sections (Current Assets, Fixed Assets, Current Liabilities, Long-term Liabilities, Share Capital, Reserves) based on AccountCode prefix conventions.

#### 2.3.5 Acceptance Criteria
- AC-BS-01: Balance Sheet balances (Assets = Liabilities + Equity).
- AC-BS-02: Retained Earnings matches cumulative P&L net profit.
- AC-BS-03: Comparative date shows prior-period balances for trend analysis.
- AC-BS-04: Header accounts aggregate their children correctly.
- AC-BS-05: Report handles a Chart of Accounts with 300+ accounts within 3 seconds.

---

### 2.4 Account Reconciliation

#### 2.4.1 Description
Enable matching of bank statement entries against journal entries for a given bank account, supporting manual and rule-based auto-matching.

#### 2.4.2 New Entities

```csharp
// Domain/Finance/BankStatement.cs
public class BankStatement : BaseEntity
{
    public Guid BankAccountId { get; private set; }
    public DateTimeOffset StatementDate { get; private set; }
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public decimal OpeningBalance { get; private set; }
    public decimal ClosingBalance { get; private set; }
    public string? Reference { get; private set; }
    public BankStatementStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public Guid CreatedBy { get; private set; }

    public BankAccount BankAccount { get; private set; } = default!;
    public ICollection<BankStatementLine> Lines { get; private set; }
        = new List<BankStatementLine>();
}

public enum BankStatementStatus
{
    Imported,
    InProgress,
    Reconciled
}

// Domain/Finance/BankStatementLine.cs
public class BankStatementLine : BaseEntity
{
    public Guid BankStatementId { get; private set; }
    public DateTimeOffset TransactionDate { get; private set; }
    public DateTimeOffset? ValueDate { get; private set; }
    public string? Description { get; private set; }
    public string? Reference { get; private set; }
    public decimal Amount { get; private set; }          // positive = credit, negative = debit
    public decimal RunningBalance { get; private set; }
    public ReconciliationStatus ReconciliationStatus { get; private set; }
    public Guid? MatchedJournalEntryId { get; private set; }
    public DateTimeOffset? ReconciledAt { get; private set; }
    public Guid? ReconciledBy { get; private set; }

    public BankStatement BankStatement { get; private set; } = default!;
    public JournalEntry? MatchedJournalEntry { get; private set; }
}

public enum ReconciliationStatus
{
    Unmatched,
    AutoMatched,
    ManuallyMatched,
    Excluded
}

// Domain/Finance/ReconciliationRule.cs
public class ReconciliationRule : BaseEntity
{
    public string Name { get; private set; } = default!;
    public Guid BankAccountId { get; private set; }
    public string MatchField { get; private set; } = default!;  // "Description", "Reference", "Amount"
    public string MatchType { get; private set; } = default!;   // "Exact", "Contains", "Regex"
    public string MatchValue { get; private set; } = default!;
    public Guid? DefaultAccountId { get; private set; }
    public bool IsActive { get; private set; }
    public int Priority { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}
```

#### 2.4.3 API Endpoints

```
POST   /api/v1/finance/bank-statements
       Body: { bankAccountId, file (CSV/MT940), format }
       -> Import bank statement

GET    /api/v1/finance/bank-statements/{id}
       -> Get statement with lines and match status

POST   /api/v1/finance/bank-statements/{id}/auto-match
       -> Run auto-matching rules against unmatched lines

POST   /api/v1/finance/bank-statements/{statementId}/lines/{lineId}/match
       Body: { journalEntryId }
       -> Manually match a statement line to a journal entry

POST   /api/v1/finance/bank-statements/{statementId}/lines/{lineId}/unmatch
       -> Remove a match

POST   /api/v1/finance/bank-statements/{statementId}/lines/{lineId}/create-entry
       Body: { accountId, description }
       -> Create a journal entry from an unmatched bank statement line

POST   /api/v1/finance/bank-statements/{id}/reconcile
       -> Finalize reconciliation (all lines must be matched or excluded)

GET    /api/v1/finance/reconciliation-rules?bankAccountId={guid}
POST   /api/v1/finance/reconciliation-rules
PUT    /api/v1/finance/reconciliation-rules/{id}
DELETE /api/v1/finance/reconciliation-rules/{id}
```

#### 2.4.4 Business Rules
- BR-RC-01: Bank statements can be imported from CSV (Georgian bank formats: Bank of Georgia, TBC Bank) and MT940 (SWIFT standard).
- BR-RC-02: Auto-matching first attempts exact amount + date matching within +/- 3 business days.
- BR-RC-03: Auto-matching then applies user-defined `ReconciliationRule` entries in priority order.
- BR-RC-04: A journal entry can only be matched to one bank statement line (1:1 relationship).
- BR-RC-05: Reconciliation cannot be finalized if the closing balance on the statement does not equal the GL balance for the bank account +/- unmatched items.
- BR-RC-06: Statement lines marked as "Excluded" do not affect the reconciliation balance check.
- BR-RC-07: Creating a journal entry from a statement line auto-matches it and posts it (if auto-post is enabled for the bank account).

#### 2.4.5 Acceptance Criteria
- AC-RC-01: CSV import from Bank of Georgia and TBC Bank formats parses correctly (date, amount, description, reference).
- AC-RC-02: Auto-match identifies exact amount+date matches and marks them as `AutoMatched`.
- AC-RC-03: Manual matching allows selecting any unmatched journal entry for a bank account.
- AC-RC-04: Reconciliation finalizes only when the balance equation holds.
- AC-RC-05: Creating a journal entry from a statement line generates correct debit/credit entries against the bank GL account.
- AC-RC-06: The reconciliation summary shows: matched count, unmatched count, total difference.

---

### 2.5 Multi-Currency Support (GEL, USD, EUR)

#### 2.5.1 Description
Support transactions in GEL, USD, and EUR with automatic conversion using National Bank of Georgia (NBG) exchange rates, and maintain all reporting in GEL (functional currency).

#### 2.5.2 New Entities

```csharp
// Domain/Finance/ExchangeRate.cs
public class ExchangeRate : BaseEntity
{
    public string FromCurrency { get; private set; } = default!;   // "USD", "EUR"
    public string ToCurrency { get; private set; } = "GEL";
    public decimal Rate { get; private set; }
    public DateTimeOffset EffectiveDate { get; private set; }
    public string Source { get; private set; } = "NBG";            // National Bank of Georgia
    public DateTimeOffset CreatedAt { get; private set; }
}

// Domain/Finance/CurrencyTransaction.cs
public class CurrencyTransaction : BaseEntity
{
    public Guid JournalEntryLineId { get; private set; }
    public string OriginalCurrency { get; private set; } = default!;
    public decimal OriginalAmount { get; private set; }
    public decimal ExchangeRate { get; private set; }
    public decimal FunctionalAmount { get; private set; }          // Amount in GEL
    public DateTimeOffset RateDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public JournalEntryLine JournalEntryLine { get; private set; } = default!;
}
```

#### 2.5.3 Modifications to Existing Entities

**JournalEntryLine** -- add:
```csharp
public string Currency { get; private set; } = "GEL";
public decimal? OriginalAmount { get; private set; }
public decimal? ExchangeRate { get; private set; }
```

**BankAccount** -- already has `Currency` property (no change needed).

#### 2.5.4 API Endpoints

```
GET    /api/v1/finance/exchange-rates
       ?date={ISO8601}
       &currency={USD|EUR}
       -> Get exchange rates for a date

POST   /api/v1/finance/exchange-rates/sync
       -> Trigger manual sync from NBG API

GET    /api/v1/finance/exchange-rates/history
       ?currency={USD|EUR}
       &from={ISO8601}
       &to={ISO8601}
       -> Historical rates
```

#### 2.5.5 Business Rules
- BR-FX-01: The functional (reporting) currency is always GEL.
- BR-FX-02: Exchange rates are sourced from the National Bank of Georgia (NBG) API daily.
- BR-FX-03: A background job syncs NBG rates at 09:00 Tbilisi time (UTC+4) daily.
- BR-FX-04: When creating a journal entry with a foreign currency line, the system converts to GEL using the rate on the entry date.
- BR-FX-05: If no rate exists for the exact date, the most recent prior rate is used.
- BR-FX-06: Exchange rate differences (gains/losses) on settlement are posted to a system account (configurable, default: "7100 - Foreign Exchange Gain/Loss").
- BR-FX-07: Bank accounts denominated in foreign currencies show both the foreign currency balance and GEL equivalent at the current rate.
- BR-FX-08: Supported currencies at launch: GEL, USD, EUR. Additional currencies can be added via configuration.
- BR-FX-09: All financial reports are generated in GEL by default; optional currency parameter converts display values using the period-end rate.

#### 2.5.6 Acceptance Criteria
- AC-FX-01: NBG rate sync job populates exchange rates for USD and EUR daily.
- AC-FX-02: A journal entry in USD is stored with both original USD amount and converted GEL amount.
- AC-FX-03: Trial Balance, P&L, and Balance Sheet correctly aggregate multi-currency entries in GEL.
- AC-FX-04: Exchange rate gain/loss entries are auto-generated on settlement.
- AC-FX-05: Manual rate override is available for backdated or corrected entries.

---

### 2.6 Auto-Journal Generation from POS

#### 2.6.1 Description
Automatically generate journal entries from completed POS transactions (sales, returns, voids) at daily closing, eliminating manual bookkeeping.

#### 2.6.2 Integration Point

The existing `OrderPlacedEvent` domain event (raised when `PosTransaction.Complete()` is called) is the trigger. The auto-journal handler subscribes to daily closing events.

#### 2.6.3 New Application Handler

```csharp
// Application/Finance/Commands/GeneratePosJournalEntriesCommand.cs
public record GeneratePosJournalEntriesCommand(
    Guid StoreId,
    Guid DailyClosingId,
    DateTimeOffset ClosingDate,
    Guid GeneratedBy) : IRequest<Result<JournalEntryResponse>>;
```

#### 2.6.4 Journal Entry Templates

**Sale Transaction:**
| Debit Account | Credit Account | Amount |
|---------------|----------------|--------|
| 1100 - Cash / 1110 - Card Receivable | 4000 - Sales Revenue | Subtotal |
| 1100 - Cash / 1110 - Card Receivable | 2200 - VAT Payable | VatTotal |

**Return Transaction:**
| Debit Account | Credit Account | Amount |
|---------------|----------------|--------|
| 4000 - Sales Revenue | 1100 - Cash / 1110 - Card Receivable | Subtotal |
| 2200 - VAT Payable | 1100 - Cash / 1110 - Card Receivable | VatTotal |

**Void Transaction:**
Reversal of the original sale entry.

#### 2.6.5 Business Rules
- BR-PJ-01: Auto-journal entries are generated per daily closing, not per individual POS transaction (aggregated by payment method and transaction type).
- BR-PJ-02: The source type is set to `"DailyClosing"` and source ID is the `DailyClosingId`.
- BR-PJ-03: Auto-journal entries are created in `Draft` status; the daily closing reconciliation process posts them.
- BR-PJ-04: If auto-post is enabled in system configuration, entries are posted immediately.
- BR-PJ-05: Account mappings (which GL accounts to debit/credit) are configurable per store via a `PosAccountMapping` configuration table.
- BR-PJ-06: Mixed-payment transactions split the debit side proportionally (e.g., 60% cash, 40% card).
- BR-PJ-07: Discount amounts are posted to a "4900 - Sales Discounts" contra-revenue account.
- BR-PJ-08: The generated journal entry total must match the daily closing total exactly.

#### 2.6.6 Acceptance Criteria
- AC-PJ-01: Daily closing for a store generates exactly one journal entry summarizing the day's activity.
- AC-PJ-02: Debit and credit sides balance (total debits = total credits).
- AC-PJ-03: Cash and card payment splits match the DailyClosing cash/card totals.
- AC-PJ-04: Returns are correctly recorded as reversals of revenue.
- AC-PJ-05: Re-running the command for an already-processed daily closing returns an idempotent result (no duplicate entries).

---

### 2.7 Auto-Journal Generation from Procurement

#### 2.7.1 Description
Automatically generate journal entries when goods are received (GoodsReceiptNote is completed) and when supplier invoices are matched.

#### 2.7.2 New Application Handler

```csharp
// Application/Finance/Commands/GenerateProcurementJournalCommand.cs
public record GenerateProcurementJournalCommand(
    Guid GoodsReceiptNoteId,
    Guid GeneratedBy) : IRequest<Result<JournalEntryResponse>>;
```

#### 2.7.3 Journal Entry Templates

**Goods Receipt (inventory received):**
| Debit Account | Credit Account | Amount |
|---------------|----------------|--------|
| 1300 - Inventory | 2100 - Accounts Payable | Subtotal |
| 1500 - Input VAT | 2100 - Accounts Payable | VatTotal |

**Supplier Payment:**
| Debit Account | Credit Account | Amount |
|---------------|----------------|--------|
| 2100 - Accounts Payable | 1000 - Bank / 1100 - Cash | Payment Amount |

#### 2.7.4 Business Rules
- BR-PR-01: Journal entries are generated when a `GoodsReceiptNote` status changes to `Received`.
- BR-PR-02: The source type is set to `"GoodsReceipt"` and source ID is the `GoodsReceiptNoteId`.
- BR-PR-03: Input VAT is separated and posted to the VAT input account for VAT declaration purposes.
- BR-PR-04: If the GRN is linked to a PurchaseOrder, the PO number is included in the journal description.
- BR-PR-05: Account mappings are configurable per product category (e.g., different inventory accounts for different product types).
- BR-PR-06: If the supplier invoice amount differs from the GRN amount, a price variance journal entry is auto-generated.
- BR-PR-07: Foreign currency purchases (USD/EUR) are converted to GEL using the rate on the receipt date.

#### 2.7.5 Acceptance Criteria
- AC-PR-01: Completing a goods receipt automatically creates a balanced journal entry.
- AC-PR-02: Inventory account is debited and AP account is credited for the correct amounts.
- AC-PR-03: VAT is correctly separated into the input VAT account.
- AC-PR-04: Duplicate prevention: receiving the same GRN twice does not create duplicate journal entries.
- AC-PR-05: The journal entry description includes GRN number, PO number, and supplier name.

---

### 2.8 Fiscal Period Management

#### 2.8.1 Description
Manage accounting periods (monthly by default), allowing period closure to prevent backdated entries, and year-end closing with retained earnings rollover.

#### 2.8.2 New Entity

```csharp
// Domain/Finance/FiscalPeriod.cs
public class FiscalPeriod : BaseEntity
{
    public int Year { get; private set; }
    public int Period { get; private set; }             // 1-12 for monthly, 1-4 for quarterly
    public string Name { get; private set; } = default!; // "January 2026", "Q1 2026"
    public DateTimeOffset PeriodStart { get; private set; }
    public DateTimeOffset PeriodEnd { get; private set; }
    public FiscalPeriodStatus Status { get; private set; }
    public Guid? ClosedBy { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
}

public enum FiscalPeriodStatus
{
    Open,
    Closed,
    Locked          // Permanently locked after audit
}
```

#### 2.8.3 API Endpoints

```
GET    /api/v1/finance/fiscal-periods
       ?year={int}
       -> List fiscal periods

POST   /api/v1/finance/fiscal-periods/generate
       Body: { year, periodType: "Monthly" | "Quarterly" }
       -> Generate periods for a fiscal year

POST   /api/v1/finance/fiscal-periods/{id}/close
       -> Close a period (prevents new entries with dates in this period)

POST   /api/v1/finance/fiscal-periods/{id}/reopen
       -> Reopen a closed period (requires elevated permissions)

POST   /api/v1/finance/fiscal-periods/year-end-close
       Body: { year }
       -> Execute year-end closing (transfers P&L to retained earnings)
```

#### 2.8.4 Business Rules
- BR-FP-01: Journal entries cannot be created or posted with an entry date in a closed period.
- BR-FP-02: Periods must be closed in order (cannot close March before closing February).
- BR-FP-03: Year-end closing creates a journal entry that zeros all Revenue and Expense accounts and posts the net to Retained Earnings (Equity).
- BR-FP-04: Reopening a closed period requires `Finance.Admin` permission and creates an audit log entry.
- BR-FP-05: Locked periods cannot be reopened (used after external audit is complete).
- BR-FP-06: The system generates periods for the Georgian fiscal year (January 1 - December 31).

#### 2.8.5 Acceptance Criteria
- AC-FP-01: Generating periods for year 2026 creates 12 monthly periods with correct date ranges.
- AC-FP-02: Attempting to post a journal entry dated in a closed period returns a validation error.
- AC-FP-03: Year-end closing produces a balanced journal entry that zeros Revenue/Expense accounts.
- AC-FP-04: Retained Earnings balance after year-end equals the P&L net profit for the year.

---

## 3. Account Mapping Configuration

### 3.1 New Entity

```csharp
// Domain/Finance/AccountMapping.cs
public class AccountMapping : BaseEntity
{
    public string MappingType { get; private set; } = default!;   // "POS", "Procurement", "Inventory"
    public string TransactionType { get; private set; } = default!; // "Sale", "Return", "GoodsReceipt"
    public string AccountRole { get; private set; } = default!;     // "Revenue", "CashDebit", "CardDebit", "VatPayable", etc.
    public Guid AccountId { get; private set; }
    public Guid? StoreId { get; private set; }                      // null = default for all stores
    public Guid? CategoryId { get; private set; }                   // null = default for all categories
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    public ChartOfAccount Account { get; private set; } = default!;
}
```

### 3.2 API Endpoints

```
GET    /api/v1/finance/account-mappings?mappingType={string}
POST   /api/v1/finance/account-mappings
PUT    /api/v1/finance/account-mappings/{id}
DELETE /api/v1/finance/account-mappings/{id}
```

---

## 4. Required Permissions

| Permission | Description |
|------------|-------------|
| `Finance.Reports.View` | View financial reports (Trial Balance, P&L, Balance Sheet) |
| `Finance.Reports.Export` | Export financial reports to PDF/Excel |
| `Finance.Reconciliation.View` | View bank reconciliation |
| `Finance.Reconciliation.Execute` | Perform reconciliation actions |
| `Finance.FiscalPeriod.Manage` | Open/close fiscal periods |
| `Finance.FiscalPeriod.Lock` | Lock fiscal periods (admin only) |
| `Finance.AccountMapping.Manage` | Configure GL account mappings |
| `Finance.ExchangeRate.Manage` | Override exchange rates |
| `Finance.AutoJournal.Configure` | Configure auto-journal settings |

---

## 5. Background Jobs

| Job | Schedule | Description |
|-----|----------|-------------|
| `NbgExchangeRateSyncJob` | Daily at 09:00 UTC+4 | Sync USD/EUR rates from NBG API |
| `PosAutoJournalJob` | After each daily closing | Generate journal entries from daily closings |
| `ReconciliationReminderJob` | Weekly on Monday | Alert finance team of unreconciled bank statements |
| `PeriodCloseReminderJob` | 5th of each month | Remind to close the prior month's period |

---

## 6. Database Migration Considerations

### 6.1 New Tables
- `bank_statements` -- imported bank statements
- `bank_statement_lines` -- individual transactions within a statement
- `reconciliation_rules` -- auto-matching rules
- `exchange_rates` -- daily currency rates
- `currency_transactions` -- foreign currency detail per journal line
- `fiscal_periods` -- accounting period management
- `account_mappings` -- GL account mapping configuration

### 6.2 Schema Changes to Existing Tables
- `journal_entry_lines` -- add `Currency`, `OriginalAmount`, `ExchangeRate` columns (nullable, default GEL)

### 6.3 Seed Data
- Default Chart of Accounts following Georgian accounting standards
- Default POS account mappings
- Default Procurement account mappings
- Exchange rates for the past 30 days (initial sync)
- Fiscal periods for the current year

---

## 7. Testing Strategy

### 7.1 Unit Tests
- Trial Balance calculation with known journal entries
- P&L section aggregation
- Balance Sheet balancing equation
- Exchange rate conversion logic
- Auto-journal entry template generation
- Period close validation rules

### 7.2 Integration Tests
- End-to-end: POS daily closing -> auto-journal generation -> Trial Balance verification
- End-to-end: GRN completion -> auto-journal generation -> AP balance verification
- Bank statement CSV import -> auto-match -> reconciliation finalization
- NBG API rate sync (with mock HTTP client)

### 7.3 Performance Tests
- Trial Balance with 1,000 accounts and 100,000 journal entries: < 5 seconds
- Balance Sheet generation: < 3 seconds
- Bank statement import with 5,000 lines: < 10 seconds
