using System.Security.Cryptography;
using System.Text;
using GeorgiaERP.Application.Common;
using Microsoft.Extensions.Configuration;

namespace GeorgiaERP.Infrastructure.Identity;

public sealed class AesTotpSecretProtector : ITotpSecretProtector
{
    private readonly byte[] _key;

    public AesTotpSecretProtector(IConfiguration configuration)
    {
        var secret = configuration["Authentication:TotpEncryptionKey"];
        if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32 || secret.Contains("${", StringComparison.Ordinal))
            throw new InvalidOperationException("Authentication:TotpEncryptionKey must be a resolved secret containing at least 32 characters.");
        _key = SHA256.HashData(Encoding.UTF8.GetBytes(secret));
    }

    public string Protect(string secret)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var plaintext = Encoding.UTF8.GetBytes(secret);
        var ciphertext = new byte[plaintext.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Encrypt(nonce, plaintext, ciphertext, tag);
        return $"v1.{Convert.ToBase64String(nonce)}.{Convert.ToBase64String(tag)}.{Convert.ToBase64String(ciphertext)}";
    }

    public string Unprotect(string protectedSecret)
    {
        // Legacy releases stored Base32 secrets directly. Authentication migrates
        // these to the protected format after the first successful verification.
        if (!protectedSecret.StartsWith("v1.", StringComparison.Ordinal))
            return protectedSecret;
        var parts = protectedSecret.Split('.');
        if (parts.Length != 4 || parts[0] != "v1")
            throw new CryptographicException("Unsupported protected TOTP secret.");
        var nonce = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var ciphertext = Convert.FromBase64String(parts[3]);
        var plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(_key, tag.Length);
        aes.Decrypt(nonce, ciphertext, tag, plaintext);
        return Encoding.UTF8.GetString(plaintext);
    }
}
