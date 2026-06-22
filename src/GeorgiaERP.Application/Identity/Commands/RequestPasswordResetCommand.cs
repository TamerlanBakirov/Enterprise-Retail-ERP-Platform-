using System.Security.Cryptography;
using GeorgiaERP.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GeorgiaERP.Application.Identity.Commands;

public record RequestPasswordResetCommand(string Email) : IRequest<Result>;

public class RequestPasswordResetCommandHandler : IRequestHandler<RequestPasswordResetCommand, Result>
{
    private readonly IAppDbContext _dbContext;
    private readonly IEmailService _emailService;
    private readonly ILogger<RequestPasswordResetCommandHandler> _logger;

    public RequestPasswordResetCommandHandler(
        IAppDbContext dbContext,
        IEmailService emailService,
        ILogger<RequestPasswordResetCommandHandler> logger)
    {
        _dbContext = dbContext;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<Result> Handle(RequestPasswordResetCommand request, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Password reset requested for non-existent or inactive email {Email}", request.Email);
            return Result.Success();
        }

        var tokenBytes = RandomNumberGenerator.GetBytes(32);
        var rawToken = Convert.ToBase64String(tokenBytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        var tokenHash = Convert.ToHexString(SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(rawToken))).ToLowerInvariant();
        var expiry = DateTimeOffset.UtcNow.AddHours(1);

        user.SetResetToken(tokenHash, expiry);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var emailMessage = EmailTemplates.PasswordReset(user.Email, rawToken, user.Username);

        try
        {
            await _emailService.SendAsync(emailMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", request.Email);
        }

        return Result.Success();
    }
}
