using System.Globalization;

namespace GeorgiaERP.Api.Middleware;

public class LocalizationMiddleware
{
    private readonly RequestDelegate _next;
    private static readonly string[] SupportedLanguages = ["ka", "en"];

    public LocalizationMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        var lang = ResolveLanguage(context.Request);
        context.Items["Lang"] = lang;

        var culture = lang == "ka"
            ? new CultureInfo("ka-GE")
            : new CultureInfo("en-US");

        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        await _next(context);
    }

    private static string ResolveLanguage(HttpRequest request)
    {
        if (request.Query.TryGetValue("lang", out var queryLang))
        {
            var ql = queryLang.ToString().ToLowerInvariant();
            if (SupportedLanguages.Contains(ql)) return ql;
        }

        var acceptLanguage = request.Headers.AcceptLanguage.ToString();
        if (!string.IsNullOrEmpty(acceptLanguage))
        {
            var preferred = acceptLanguage
                .Split(',')
                .Select(s => s.Split(';')[0].Trim().ToLowerInvariant())
                .Select(s => s.Length >= 2 ? s[..2] : s)
                .FirstOrDefault(s => SupportedLanguages.Contains(s));

            if (preferred is not null) return preferred;
        }

        return "ka";
    }
}

public static class HttpContextLocalizationExtensions
{
    public static string GetLanguage(this HttpContext context) =>
        context.Items["Lang"] as string ?? "ka";

    public static string Localized(this HttpContext context, string? name, string? nameKa) =>
        context.GetLanguage() == "ka" ? (nameKa ?? name ?? "") : (name ?? nameKa ?? "");

    public static string Localized(this HttpContext context, string? firstName, string? lastName,
        string? firstNameKa, string? lastNameKa)
    {
        return context.GetLanguage() == "ka"
            ? $"{firstNameKa ?? firstName} {lastNameKa ?? lastName}".Trim()
            : $"{firstName ?? firstNameKa} {lastName ?? lastNameKa}".Trim();
    }
}
