namespace GeorgiaERP.Application.Common;

/// <summary>
/// Centralized email templates for the ERP platform. Templates use simple
/// string interpolation for performance and zero external dependencies.
/// Georgian (ka-GE) versions are provided alongside English for bilingual support.
/// </summary>
public static class EmailTemplates
{
    private const string BaseStyle = """
        <style>
            body { font-family: 'Segoe UI', Tahoma, sans-serif; color: #333; line-height: 1.6; }
            .container { max-width: 600px; margin: 0 auto; padding: 20px; }
            .header { background: #1a5276; color: white; padding: 20px; text-align: center; border-radius: 4px 4px 0 0; }
            .content { background: #f9f9f9; padding: 20px; border: 1px solid #ddd; }
            .footer { padding: 15px; text-align: center; font-size: 12px; color: #888; }
            .alert-critical { border-left: 4px solid #e74c3c; padding: 10px; margin: 10px 0; background: #fdf2f2; }
            .alert-warning { border-left: 4px solid #f39c12; padding: 10px; margin: 10px 0; background: #fef9e7; }
            .alert-info { border-left: 4px solid #3498db; padding: 10px; margin: 10px 0; background: #eaf2f8; }
            .btn { display: inline-block; padding: 10px 20px; background: #1a5276; color: white; text-decoration: none; border-radius: 4px; margin: 10px 0; }
            table { width: 100%; border-collapse: collapse; margin: 10px 0; }
            th, td { padding: 8px 12px; text-align: left; border-bottom: 1px solid #ddd; }
            th { background: #ecf0f1; font-weight: 600; }
        </style>
        """;

    private static string Wrap(string title, string body) => $"""
        <!DOCTYPE html>
        <html>
        <head><meta charset="utf-8">{BaseStyle}</head>
        <body>
        <div class="container">
            <div class="header"><h2>{title}</h2></div>
            <div class="content">{body}</div>
            <div class="footer">
                Georgia ERP Platform &mdash; Enterprise Retail Management<br/>
                საქართველოს ERP პლატფორმა
            </div>
        </div>
        </body>
        </html>
        """;

    /// <summary>
    /// Email sent when an RS.GE waybill submission fails after all retries.
    /// </summary>
    public static EmailMessage WaybillSubmissionFailed(
        string recipientEmail,
        string waybillNumber,
        string errorMessage,
        int retryCount,
        DateTime failedAt)
    {
        var body = $"""
            <div class="alert-critical">
                <strong>RS.GE ზედნადების წარდგენა ვერ მოხერხდა / Waybill Submission Failed</strong>
            </div>
            <p>Waybill <strong>{waybillNumber}</strong> failed to submit to RS.GE after {retryCount} attempts.</p>
            <table>
                <tr><th>Waybill Number</th><td>{waybillNumber}</td></tr>
                <tr><th>Error</th><td>{errorMessage}</td></tr>
                <tr><th>Retry Count</th><td>{retryCount}</td></tr>
                <tr><th>Failed At</th><td>{failedAt:yyyy-MM-dd HH:mm:ss}</td></tr>
            </table>
            <p>Please investigate the issue and resubmit manually if needed.</p>
            <p><em>გთხოვთ, გადაამოწმოთ პრობლემა და საჭიროების შემთხვევაში ხელახლა წარადგინოთ.</em></p>
            """;

        return new EmailMessage
        {
            To = recipientEmail,
            Subject = $"[CRITICAL] RS.GE Waybill Submission Failed: {waybillNumber}",
            HtmlBody = Wrap("Waybill Submission Failed", body),
            PlainTextBody = $"Waybill {waybillNumber} failed to submit to RS.GE after {retryCount} attempts. Error: {errorMessage}. Failed at: {failedAt:yyyy-MM-dd HH:mm:ss}.",
            Priority = EmailPriority.High,
            Tag = "waybill-failure"
        };
    }

    /// <summary>
    /// Email sent when stock levels fall below minimum thresholds.
    /// </summary>
    public static EmailMessage LowStockAlert(
        string recipientEmail,
        IReadOnlyList<LowStockItem> items)
    {
        var outOfStock = items.Where(i => i.QuantityOnHand <= 0).ToList();
        var belowMinimum = items.Where(i => i.QuantityOnHand > 0).ToList();

        var rows = string.Join("\n", items.Select(i =>
        {
            var severity = i.QuantityOnHand <= 0 ? "🔴" : "🟡";
            return $"<tr><td>{severity}</td><td>{i.Sku}</td><td>{i.ProductName}</td><td>{i.WarehouseName}</td><td>{i.QuantityOnHand}</td><td>{i.MinStockLevel}</td></tr>";
        }));

        var body = $"""
            <div class="alert-warning">
                <strong>მარაგის გაფრთხილება / Low Stock Alert</strong>
            </div>
            <p>{outOfStock.Count} item(s) out of stock, {belowMinimum.Count} item(s) below minimum level.</p>
            <table>
                <tr><th></th><th>SKU</th><th>Product</th><th>Warehouse</th><th>Qty</th><th>Min</th></tr>
                {rows}
            </table>
            <p>Please review and reorder as needed.</p>
            """;

        return new EmailMessage
        {
            To = recipientEmail,
            Subject = $"[ALERT] Low Stock: {outOfStock.Count} out of stock, {belowMinimum.Count} below minimum",
            HtmlBody = Wrap("Low Stock Alert", body),
            PlainTextBody = $"{outOfStock.Count} items out of stock, {belowMinimum.Count} items below minimum level.",
            Priority = outOfStock.Count > 0 ? EmailPriority.High : EmailPriority.Normal,
            Tag = "low-stock-alert"
        };
    }

    /// <summary>
    /// Welcome email sent when a new user is registered.
    /// </summary>
    public static EmailMessage UserRegistered(
        string recipientEmail,
        string username,
        string fullName,
        string? temporaryPassword = null)
    {
        var passwordSection = temporaryPassword is not null
            ? $"""
                <div class="alert-info">
                    <strong>Temporary Password / დროებითი პაროლი:</strong> <code>{temporaryPassword}</code><br/>
                    Please change your password after first login.
                </div>
                """
            : "";

        var body = $"""
            <p>მოგესალმებით / Welcome, <strong>{fullName}</strong>!</p>
            <p>Your account has been created on the Georgia ERP Platform.</p>
            <table>
                <tr><th>Username</th><td>{username}</td></tr>
                <tr><th>Email</th><td>{recipientEmail}</td></tr>
            </table>
            {passwordSection}
            <p>We recommend enabling Two-Factor Authentication (2FA) for enhanced security.</p>
            <p><em>უსაფრთხოების გასაუმჯობესებლად გირჩევთ ორფაქტორიანი ავთენტიფიკაციის ჩართვას.</em></p>
            """;

        return new EmailMessage
        {
            To = recipientEmail,
            Subject = "Welcome to Georgia ERP Platform / კეთილი იყოს თქვენი მობრძანება",
            HtmlBody = Wrap("Welcome / მოგესალმებით", body),
            PlainTextBody = $"Welcome {fullName}! Your account ({username}) has been created on the Georgia ERP Platform.",
            Priority = EmailPriority.Normal,
            Tag = "user-registration"
        };
    }

    /// <summary>
    /// Password reset email with a reset token/link.
    /// </summary>
    public static EmailMessage PasswordReset(
        string recipientEmail,
        string username,
        string resetToken,
        string resetUrl)
    {
        var body = $"""
            <p>A password reset was requested for your account <strong>{username}</strong>.</p>
            <div class="alert-info">
                <strong>Reset Code / აღდგენის კოდი:</strong> <code>{resetToken}</code>
            </div>
            <p>Click the button below or enter the reset code manually:</p>
            <p><a href="{resetUrl}" class="btn">Reset Password / პაროლის აღდგენა</a></p>
            <p>This link expires in 1 hour. If you did not request this reset, please ignore this email and ensure your account is secure.</p>
            <p><em>ეს ბმული ვადაგასულია 1 საათში. თუ თქვენ არ მოითხოვეთ აღდგენა, გთხოვთ უგულებელყოთ ეს წერილი.</em></p>
            """;

        return new EmailMessage
        {
            To = recipientEmail,
            Subject = "Password Reset Request / პაროლის აღდგენა - Georgia ERP",
            HtmlBody = Wrap("Password Reset / პაროლის აღდგენა", body),
            PlainTextBody = $"A password reset was requested for account {username}. Reset code: {resetToken}. This code expires in 1 hour.",
            Priority = EmailPriority.High,
            Tag = "password-reset"
        };
    }
}

/// <summary>
/// Low stock item data for email template rendering.
/// </summary>
public sealed record LowStockItem(
    string Sku,
    string ProductName,
    string WarehouseName,
    decimal QuantityOnHand,
    decimal MinStockLevel);
