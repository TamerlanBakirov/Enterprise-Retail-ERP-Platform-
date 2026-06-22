using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GeorgiaERP.Application.Licensing;
using Microsoft.Extensions.Configuration;

namespace GeorgiaERP.Infrastructure.Licensing;

public sealed class HmacLicenseKeyValidator : ILicenseKeyValidator
{
    private readonly byte[] _signingKey;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public HmacLicenseKeyValidator(IConfiguration configuration)
    {
        var key = configuration["Licensing:SigningKey"];
        if (string.IsNullOrWhiteSpace(key) || key.Length < 32)
            throw new InvalidOperationException("Licensing:SigningKey must contain at least 32 characters.");
        _signingKey = Encoding.UTF8.GetBytes(key);
    }

    public LicenseKeyValidationResult Validate(string licenseKey)
    {
        try
        {
            var parts = licenseKey.Split('.', 2);
            if (parts.Length != 2) return LicenseKeyValidationResult.Invalid("Malformed license key.");

            var payloadBytes = Decode(parts[0]);
            var suppliedSignature = Decode(parts[1]);
            var expectedSignature = HMACSHA256.HashData(_signingKey, payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(expectedSignature, suppliedSignature))
                return LicenseKeyValidationResult.Invalid("Invalid license signature.");

            var payload = JsonSerializer.Deserialize<LicensePayload>(payloadBytes, JsonOptions);
            if (payload is null || string.IsNullOrWhiteSpace(payload.CompanyName) || payload.ExpiresAt <= DateTimeOffset.UtcNow ||
                payload.MaxUsers < 1 || payload.MaxStores < 1)
                return LicenseKeyValidationResult.Invalid("License claims are invalid or expired.");

            return new LicenseKeyValidationResult(true, payload.CompanyName, payload.ExpiresAt,
                payload.MaxUsers, payload.MaxStores, null);
        }
        catch (Exception ex) when (ex is FormatException or JsonException)
        {
            return LicenseKeyValidationResult.Invalid("Malformed license key.");
        }
    }

    public static string CreateKey(string signingKey, string companyName, DateTimeOffset expiresAt, int maxUsers, int maxStores)
    {
        var payload = JsonSerializer.SerializeToUtf8Bytes(
            new LicensePayload(companyName, expiresAt, maxUsers, maxStores), JsonOptions);
        return $"{Encode(payload)}.{Encode(HMACSHA256.HashData(Encoding.UTF8.GetBytes(signingKey), payload))}";
    }

    private static string Encode(byte[] value) => Convert.ToBase64String(value).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    private static byte[] Decode(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        padded += new string('=', (4 - padded.Length % 4) % 4);
        return Convert.FromBase64String(padded);
    }

    private sealed record LicensePayload(string CompanyName, DateTimeOffset ExpiresAt, int MaxUsers, int MaxStores);
}
