using GeorgiaERP.Domain.Common;

namespace GeorgiaERP.Domain.POS;

public enum TerminalType
{
    Register,
    SelfService,
    Mobile
}

public class PosTerminal : BaseEntity
{
    public string Code { get; private set; } = default!;
    public Guid StoreId { get; private set; }
    public string Name { get; private set; } = default!;
    public TerminalType TerminalType { get; private set; }
    public bool IsActive { get; private set; }
    public string? Settings { get; private set; } // jsonb
    public DateTimeOffset CreatedAt { get; private set; }

    // Navigation properties
    public ICollection<PosSession> Sessions { get; private set; } = new List<PosSession>();

    private PosTerminal() { }

    public static PosTerminal Create(string code, Guid storeId, string name, TerminalType terminalType)
    {
        return new PosTerminal
        {
            Code = code,
            StoreId = storeId,
            Name = name,
            TerminalType = terminalType,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;
}
