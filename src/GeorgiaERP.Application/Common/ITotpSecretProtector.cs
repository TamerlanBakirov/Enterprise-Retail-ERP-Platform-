namespace GeorgiaERP.Application.Common;

public interface ITotpSecretProtector
{
    string Protect(string secret);
    string Unprotect(string protectedSecret);
}
