namespace GeorgiaERP.Application.Identity.DTOs;

public record LoginRequest(string Username, string Password, string? TwoFactorCode = null);
