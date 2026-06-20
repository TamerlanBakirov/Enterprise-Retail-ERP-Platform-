namespace GeorgiaERP.Application.Finance.DTOs;

/// <summary>
/// Represents a chart of accounts entry for API responses.
/// </summary>
public record ChartOfAccountDto(
    Guid Id,
    string AccountCode,
    string Name,
    string? NameKa,
    string AccountType,
    string BalanceType,
    Guid? ParentId,
    bool IsHeader,
    bool IsSystem,
    bool IsActive);

/// <summary>
/// Represents a journal entry for API responses.
/// </summary>
public record JournalEntryDto(
    Guid Id,
    string EntryNumber,
    DateTimeOffset EntryDate,
    string? Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    DateTimeOffset? PostedAt,
    DateTimeOffset CreatedAt);

/// <summary>
/// Represents a journal entry with its line items for detailed API responses.
/// </summary>
public record JournalEntryDetailDto(
    Guid Id,
    string EntryNumber,
    DateTimeOffset EntryDate,
    string? Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string? SourceType,
    Guid? SourceId,
    DateTimeOffset? PostedAt,
    Guid CreatedBy,
    DateTimeOffset CreatedAt,
    IReadOnlyList<JournalEntryLineDto> Lines);

/// <summary>
/// Represents a single line in a journal entry.
/// </summary>
public record JournalEntryLineDto(
    Guid Id,
    int LineNumber,
    Guid AccountId,
    string? AccountCode,
    string? AccountName,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description);

/// <summary>
/// Represents a bank account for API responses.
/// </summary>
public record BankAccountDto(
    Guid Id,
    string AccountName,
    string BankName,
    string AccountNumber,
    string? Iban,
    string Currency,
    decimal CurrentBalance,
    Guid? GlAccountId,
    bool IsActive);
