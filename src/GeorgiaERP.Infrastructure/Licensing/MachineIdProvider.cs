using System.Security.Cryptography;
using System.Text;
using GeorgiaERP.Application.Licensing;

namespace GeorgiaERP.Infrastructure.Licensing;

public class MachineIdProviderService : IMachineIdProvider
{
    public string GetMachineId() => MachineIdProvider.GetMachineId();
}

public static class MachineIdProvider
{
    public static string GetMachineId()
    {
        var raw = $"{Environment.MachineName}-{Environment.OSVersion}-{Environment.ProcessorCount}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexStringLower(hash)[..32];
    }
}
