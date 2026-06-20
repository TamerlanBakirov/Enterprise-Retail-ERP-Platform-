namespace GeorgiaERP.Desktop.Models;

public record AccountDto(
    Guid Id,
    string Code,
    string Name,
    string? NameKa,
    string AccountType,
    Guid? ParentId,
    bool IsSystem,
    bool IsActive);

public record JournalEntryDto(
    Guid Id,
    string EntryNumber,
    DateTimeOffset EntryDate,
    string Description,
    string Status,
    decimal TotalDebit,
    decimal TotalCredit,
    string? SourceType,
    Guid? SourceId,
    DateTimeOffset CreatedAt);

public record BankAccountDto(
    Guid Id,
    string Name,
    string BankName,
    string AccountNumber,
    string? Iban,
    string Currency,
    decimal Balance,
    Guid? GlAccountId,
    bool IsActive);

public record CreateAccountRequest(
    string Code,
    string Name,
    string? NameKa,
    string AccountType,
    Guid? ParentId);

public record CreateJournalEntryRequest(
    string Description,
    DateTimeOffset EntryDate,
    List<JournalEntryLineRequest> Lines);

public record JournalEntryLineRequest(
    Guid AccountId,
    decimal DebitAmount,
    decimal CreditAmount,
    string? Description);

public record CreateBankAccountRequest(
    string Name,
    string BankName,
    string AccountNumber,
    string? Iban,
    string Currency,
    decimal InitialBalance,
    Guid? GlAccountId);
