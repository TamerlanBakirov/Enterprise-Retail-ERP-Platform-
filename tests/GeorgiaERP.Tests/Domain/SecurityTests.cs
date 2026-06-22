using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using GeorgiaERP.Domain.Identity;
using GeorgiaERP.Infrastructure.Identity;
using GeorgiaERP.Infrastructure.Licensing;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace GeorgiaERP.Tests.Domain;

public class SecurityTests
{
    [Fact]
    public void User_IsLocked_AfterConfiguredFailedAttempts()
    {
        var user = User.Create("cashier", "cashier@test.local", "hash", "Test", "User");

        for (var i = 0; i < 5; i++)
            user.RecordFailedLogin(5, TimeSpan.FromMinutes(15));

        user.FailedLoginCount.Should().Be(5);
        user.LockedUntil.Should().BeAfter(DateTimeOffset.UtcNow);
    }

    [Fact]
    public void TotpVerifier_AcceptsCurrentCode_AndRejectsWrongCode()
    {
        const string secret = "JBSWY3DPEHPK3PXP";
        var now = DateTimeOffset.UtcNow;
        var code = GenerateTotp(secret, now);
        var verifier = new TotpVerifier();

        verifier.Verify(secret, code, now).Should().BeTrue();
        verifier.Verify(secret, code == "000000" ? "999999" : "000000", now).Should().BeFalse();
    }

    [Fact]
    public void LicenseValidator_RejectsTamperedKey()
    {
        const string signingKey = "TEST-LICENSE-SIGNING-KEY-WITH-MORE-THAN-32-CHARACTERS";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Licensing:SigningKey"] = signingKey
        }).Build();
        var validator = new HmacLicenseKeyValidator(configuration);
        var key = HmacLicenseKeyValidator.CreateKey(signingKey, "Acme LLC", DateTimeOffset.UtcNow.AddDays(30), 5, 1);

        validator.Validate(key).IsValid.Should().BeTrue();
        validator.Validate(key[..^1] + (key[^1] == 'A' ? 'B' : 'A')).IsValid.Should().BeFalse();
    }

    [Fact]
    public void TotpSecretProtector_EncryptsAndRestoresSecret()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:TotpEncryptionKey"] = "TEST-TOTP-ENCRYPTION-KEY-WITH-MORE-THAN-32-CHARACTERS"
        }).Build();
        var protector = new AesTotpSecretProtector(configuration);

        var encrypted = protector.Protect("JBSWY3DPEHPK3PXP");

        encrypted.Should().NotContain("JBSWY3DPEHPK3PXP");
        protector.Unprotect(encrypted).Should().Be("JBSWY3DPEHPK3PXP");
    }

    private static string GenerateTotp(string base32Secret, DateTimeOffset now)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var buffer = 0;
        var bits = 0;
        var bytes = new List<byte>();
        foreach (var character in base32Secret)
        {
            buffer = (buffer << 5) | alphabet.IndexOf(character);
            bits += 5;
            if (bits < 8) continue;
            bits -= 8;
            bytes.Add((byte)(buffer >> bits));
            buffer &= (1 << bits) - 1;
        }

        Span<byte> counter = stackalloc byte[8];
        BinaryPrimitives.WriteInt64BigEndian(counter, now.ToUnixTimeSeconds() / 30);
        var hash = HMACSHA1.HashData(bytes.ToArray(), counter);
        var offset = hash[^1] & 0x0f;
        var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) |
                     (hash[offset + 2] << 8) | hash[offset + 3];
        return (binary % 1_000_000).ToString("D6");
    }
}
