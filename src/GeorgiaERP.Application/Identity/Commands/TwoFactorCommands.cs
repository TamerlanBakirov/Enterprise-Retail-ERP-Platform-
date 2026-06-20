using System.Security.Cryptography;
using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GeorgiaERP.Application.Identity.Commands;

public record BeginTwoFactorSetupCommand(Guid UserId) : IRequest<Result<TwoFactorSetupResponse>>;
public record ConfirmTwoFactorSetupCommand(Guid UserId, string Code) : IRequest<Result>;
public record DisableTwoFactorCommand(Guid UserId, string Code) : IRequest<Result>;
public record TwoFactorSetupResponse(string Secret, string OtpAuthUri);

public sealed class BeginTwoFactorSetupCommandHandler : IRequestHandler<BeginTwoFactorSetupCommand, Result<TwoFactorSetupResponse>>
{
    private readonly IAppDbContext _dbContext;
    private readonly ITotpSecretProtector _protector;
    public BeginTwoFactorSetupCommandHandler(IAppDbContext dbContext, ITotpSecretProtector protector) =>
        (_dbContext, _protector) = (dbContext, protector);

    public async Task<Result<TwoFactorSetupResponse>> Handle(BeginTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user is null) return Result.Failure<TwoFactorSetupResponse>("User not found.");

        var secret = EncodeBase32(RandomNumberGenerator.GetBytes(20));
        user.BeginTwoFactorSetup(_protector.Protect(secret));
        await _dbContext.SaveChangesAsync(cancellationToken);
        var issuer = Uri.EscapeDataString("Georgia ERP");
        var account = Uri.EscapeDataString(user.Username);
        return Result.Success(new TwoFactorSetupResponse(secret,
            $"otpauth://totp/{issuer}:{account}?secret={secret}&issuer={issuer}&digits=6&period=30"));
    }

    private static string EncodeBase32(byte[] bytes)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var output = new System.Text.StringBuilder((bytes.Length * 8 + 4) / 5);
        var buffer = 0;
        var bits = 0;
        foreach (var value in bytes)
        {
            buffer = (buffer << 8) | value;
            bits += 8;
            while (bits >= 5)
            {
                bits -= 5;
                output.Append(alphabet[(buffer >> bits) & 31]);
                buffer &= (1 << bits) - 1;
            }
        }
        if (bits > 0) output.Append(alphabet[(buffer << (5 - bits)) & 31]);
        return output.ToString();
    }
}

public sealed class ConfirmTwoFactorSetupCommandHandler : IRequestHandler<ConfirmTwoFactorSetupCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly ITotpVerifier _verifier;
    private readonly ITotpSecretProtector _protector;
    public ConfirmTwoFactorSetupCommandHandler(IAppDbContext dbContext, ITotpVerifier verifier, ITotpSecretProtector protector) =>
        (_dbContext, _verifier, _protector) = (dbContext, verifier, protector);

    public async Task<Result> Handle(ConfirmTwoFactorSetupCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user?.TotpSecret is null) return Result.Failure("Two-factor setup has not been started.");
        if (!_verifier.Verify(_protector.Unprotect(user.TotpSecret), request.Code, DateTimeOffset.UtcNow))
            return Result.Failure("Invalid two-factor authentication code.");
        user.ConfirmTwoFactorSetup();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}

public sealed class DisableTwoFactorCommandHandler : IRequestHandler<DisableTwoFactorCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly ITotpVerifier _verifier;
    private readonly ITotpSecretProtector _protector;
    public DisableTwoFactorCommandHandler(IAppDbContext dbContext, ITotpVerifier verifier, ITotpSecretProtector protector) =>
        (_dbContext, _verifier, _protector) = (dbContext, verifier, protector);

    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user?.TotpSecret is null || !user.Is2FaEnabled) return Result.Failure("Two-factor authentication is not enabled.");
        if (!_verifier.Verify(_protector.Unprotect(user.TotpSecret), request.Code, DateTimeOffset.UtcNow))
            return Result.Failure("Invalid two-factor authentication code.");
        user.DisableTwoFactor();
        await _dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
