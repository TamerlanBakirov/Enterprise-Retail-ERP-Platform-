using System.Security.Cryptography;
using System.Text;

namespace GeorgiaERP.Infrastructure.Licensing;

public static class MachineIdProvider
{
    public static string GetMachineId()
    {
        var raw = $"{Environment.MachineName}-{Environment.OSVersion}-{Environment.ProcessorCount}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
