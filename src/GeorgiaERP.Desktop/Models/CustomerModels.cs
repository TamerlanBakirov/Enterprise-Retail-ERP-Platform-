namespace GeorgiaERP.Desktop.Models;

public record CustomerDto(
    Guid Id,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string Phone,
    string? Email,
    string? CompanyName,
    string? TaxId,
    string? LoyaltyCardNumber,
    int LoyaltyPoints,
    int TotalVisits,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string Phone,
    string? Email,
    string? CompanyName,
    string? TaxId);

public record EarnPointsRequest(
    int Points,
    string? ReferenceType,
    Guid? ReferenceId,
    string? Description);

public record RedeemPointsRequest(
    int Points,
    string? Description);
