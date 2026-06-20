using System.Security.Cryptography;
using GeorgiaERP.Application.Common;

namespace GeorgiaERP.Infrastructure.Identity;

public sealed class TotpVerifier : ITotpVerifier
{
    public bool Verify(string base32Secret, string code, DateTimeOffset now)
    {
        if (code.Length != 6 || !code.All(char.IsDigit))
            return false;

        byte[] secret;
        try { secret = DecodeBase32(base32Secret); }
        catch (FormatException) { return false; }

        var counter = now.ToUnixTimeSeconds() / 30;
        Span<byte> counterBytes = stackalloc byte[8];
        for (var offset = -1; offset <= 1; offset++)
        {
            System.Buffers.Binary.BinaryPrimitives.WriteInt64BigEndian(counterBytes, counter + offset);
            var hash = HMACSHA1.HashData(secret, counterBytes);
            var index = hash[^1] & 0x0f;
            var binary = ((hash[index] & 0x7f) << 24) |
                         (hash[index + 1] << 16) |
                         (hash[index + 2] << 8) |
                         hash[index + 3];
            if (CryptographicOperations.FixedTimeEquals(
                    System.Text.Encoding.ASCII.GetBytes((binary % 1_000_000).ToString("D6")),
                    System.Text.Encoding.ASCII.GetBytes(code)))
                return true;
        }

        return false;
    }

    private static byte[] DecodeBase32(string value)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var normalized = value.Trim().TrimEnd('=').Replace(" ", "").ToUpperInvariant();
        var output = new List<byte>();
        var buffer = 0;
        var bits = 0;
        foreach (var character in normalized)
        {
            var index = alphabet.IndexOf(character);
            if (index < 0) throw new FormatException("Invalid Base32 secret.");
            buffer = (buffer << 5) | index;
            bits += 5;
            if (bits < 8) continue;
            bits -= 8;
            output.Add((byte)(buffer >> bits));
            buffer &= (1 << bits) - 1;
        }
        return output.Count == 0 ? throw new FormatException("Empty Base32 secret.") : output.ToArray();
    }
}
