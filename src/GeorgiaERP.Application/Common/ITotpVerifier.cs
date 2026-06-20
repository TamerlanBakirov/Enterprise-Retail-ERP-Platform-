namespace GeorgiaERP.Application.Common;

public interface ITotpVerifier
{
    bool Verify(string base32Secret, string code, DateTimeOffset now);
}
