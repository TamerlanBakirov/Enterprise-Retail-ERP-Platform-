namespace GeorgiaERP.Application.CRM.DTOs;

public record CustomerDto(
    Guid Id,
    string CustomerNumber,
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? CompanyName,
    string? Tin,
    string? Phone,
    string? Email,
    string? LoyaltyCardNumber,
    string? LoyaltyTier,
    int LoyaltyPoints,
    decimal TotalPurchases,
    int TotalVisits,
    DateTimeOffset? LastVisitAt,
    bool IsActive,
    DateTimeOffset CreatedAt);

public record CreateCustomerRequest(
    string FirstName,
    string LastName,
    string? FirstNameKa,
    string? LastNameKa,
    string? CompanyName,
    string? Tin,
    string? Phone,
    string? Email,
    DateTimeOffset? DateOfBirth,
    string? Gender,
    bool ConsentSms,
    bool ConsentEmail);
