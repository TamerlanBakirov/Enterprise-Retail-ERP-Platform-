namespace GeorgiaERP.Application.Common;

public static class EmailTemplates
{
    private const string BrandColor = "#1a5276";
    private const string AccentColor = "#2ecc71";
    private const string WarningColor = "#e74c3c";

    public static EmailMessage PasswordReset(string to, string resetToken, string userName)
    {
        var html = WrapLayout("Password Reset / პაროლის აღდგენა", $@"
            <h2 style=""color: {BrandColor}; margin-bottom: 8px;"">Password Reset Request</h2>
            <h3 style=""color: {BrandColor}; margin-top: 0;"">პაროლის აღდგენის მოთხოვნა</h3>
            <p>Hello <strong>{Encode(userName)}</strong>,</p>
            <p>გამარჯობა <strong>{Encode(userName)}</strong>,</p>
            <p>We received a request to reset your password. Use the token below to complete the process:</p>
            <p>მივიღეთ თქვენი პაროლის აღდგენის მოთხოვნა. გამოიყენეთ ქვემოთ მოცემული ტოკენი:</p>
            <div style=""background: #f4f6f9; padding: 16px; border-radius: 6px; text-align: center; margin: 24px 0;"">
                <code style=""font-size: 18px; letter-spacing: 2px; color: {BrandColor};"">{Encode(resetToken)}</code>
            </div>
            <p style=""color: #888; font-size: 13px;"">This token expires in 1 hour. If you did not request a password reset, please ignore this email.</p>
            <p style=""color: #888; font-size: 13px;"">ტოკენი მოქმედებს 1 საათის განმავლობაში. თუ თქვენ არ მოითხოვეთ პაროლის აღდგენა, გთხოვთ უგულებელყოთ ეს წერილი.</p>");

        return new EmailMessage(
            to,
            "Password Reset / პაროლის აღდგენა - Georgia ERP",
            html,
            $"Password reset token for {userName}: {resetToken}. This token expires in 1 hour.");
    }

    public static EmailMessage LowStockAlert(string to, string productName, string sku, decimal currentStock, decimal minLevel)
    {
        var html = WrapLayout("Low Stock Alert / მარაგის შეტყობინება", $@"
            <h2 style=""color: {WarningColor}; margin-bottom: 8px;"">Low Stock Alert</h2>
            <h3 style=""color: {WarningColor}; margin-top: 0;"">დაბალი მარაგის შეტყობინება</h3>
            <p>The following product has fallen below its minimum stock level:</p>
            <p>შემდეგი პროდუქტის მარაგი მინიმალურ დონეზე დაბლა დაეცა:</p>
            <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                <tr style=""background: #f4f6f9;"">
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Product / პროდუქტი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{Encode(productName)}</td>
                </tr>
                <tr>
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">SKU</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{Encode(sku)}</td>
                </tr>
                <tr style=""background: #f4f6f9;"">
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Current Stock / მიმდინარე მარაგი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd; color: {WarningColor}; font-weight: bold;"">{currentStock}</td>
                </tr>
                <tr>
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Min Level / მინიმალური დონე</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{minLevel}</td>
                </tr>
            </table>
            <p>Please reorder this product as soon as possible.</p>
            <p>გთხოვთ, მოახდინოთ ამ პროდუქტის ხელახალი შეკვეთა რაც შეიძლება მალე.</p>");

        return new EmailMessage(
            to,
            $"Low Stock Alert: {productName} ({sku}) - Georgia ERP",
            html,
            $"Low stock alert: {productName} (SKU: {sku}) is at {currentStock} units, below minimum level of {minLevel}.");
    }

    public static EmailMessage OrderConfirmation(string to, string orderNumber, decimal total, string currency, DateTimeOffset date)
    {
        var formattedDate = date.ToString("dd/MM/yyyy HH:mm");
        var html = WrapLayout("Order Confirmation / შეკვეთის დადასტურება", $@"
            <h2 style=""color: {AccentColor}; margin-bottom: 8px;"">Order Confirmed</h2>
            <h3 style=""color: {AccentColor}; margin-top: 0;"">შეკვეთა დადასტურებულია</h3>
            <p>Your order has been successfully processed.</p>
            <p>თქვენი შეკვეთა წარმატებით დამუშავდა.</p>
            <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                <tr style=""background: #f4f6f9;"">
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Order # / შეკვეთა #</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{Encode(orderNumber)}</td>
                </tr>
                <tr>
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Date / თარიღი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{formattedDate}</td>
                </tr>
                <tr style=""background: #f4f6f9;"">
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Total / ჯამი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd; font-size: 18px; font-weight: bold; color: {BrandColor};"">{total:N2} {Encode(currency)}</td>
                </tr>
            </table>
            <p>Thank you for your order!</p>
            <p>მადლობა შეკვეთისთვის!</p>");

        return new EmailMessage(
            to,
            $"Order Confirmation #{orderNumber} - Georgia ERP",
            html,
            $"Order #{orderNumber} confirmed. Total: {total:N2} {currency}. Date: {formattedDate}.");
    }

    public static EmailMessage WelcomeUser(string to, string userName, string tempPassword)
    {
        var html = WrapLayout("Welcome / კეთილი იყოს თქვენი მობრძანება", $@"
            <h2 style=""color: {BrandColor}; margin-bottom: 8px;"">Welcome to Georgia ERP</h2>
            <h3 style=""color: {BrandColor}; margin-top: 0;"">კეთილი იყოს თქვენი მობრძანება Georgia ERP-ში</h3>
            <p>Hello <strong>{Encode(userName)}</strong>,</p>
            <p>გამარჯობა <strong>{Encode(userName)}</strong>,</p>
            <p>Your account has been created. Use the credentials below to log in:</p>
            <p>თქვენი ანგარიში შეიქმნა. გამოიყენეთ ქვემოთ მოცემული მონაცემები შესასვლელად:</p>
            <table style=""width: 100%; border-collapse: collapse; margin: 20px 0;"">
                <tr style=""background: #f4f6f9;"">
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Username / მომხმარებელი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;"">{Encode(userName)}</td>
                </tr>
                <tr>
                    <td style=""padding: 10px; border: 1px solid #ddd; font-weight: bold;"">Temporary Password / დროებითი პაროლი</td>
                    <td style=""padding: 10px; border: 1px solid #ddd;""><code>{Encode(tempPassword)}</code></td>
                </tr>
            </table>
            <p style=""color: {WarningColor}; font-weight: bold;"">Please change your password after your first login.</p>
            <p style=""color: {WarningColor}; font-weight: bold;"">გთხოვთ, შეცვალოთ პაროლი პირველი შესვლის შემდეგ.</p>");

        return new EmailMessage(
            to,
            "Welcome to Georgia ERP / კეთილი იყოს თქვენი მობრძანება",
            html,
            $"Welcome {userName}! Your temporary password is: {tempPassword}. Please change it after your first login.");
    }

    private static string WrapLayout(string title, string bodyContent)
    {
        return $@"<!DOCTYPE html>
<html lang=""en"">
<head><meta charset=""UTF-8""><meta name=""viewport"" content=""width=device-width, initial-scale=1.0""></head>
<body style=""margin: 0; padding: 0; background: #f0f2f5; font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;"">
    <table role=""presentation"" style=""width: 100%; background: #f0f2f5; padding: 32px 0;"">
        <tr><td align=""center"">
            <table role=""presentation"" style=""max-width: 600px; width: 100%; background: #ffffff; border-radius: 8px; overflow: hidden; box-shadow: 0 2px 8px rgba(0,0,0,0.08);"">
                <tr>
                    <td style=""background: {BrandColor}; padding: 24px 32px; text-align: center;"">
                        <h1 style=""color: #ffffff; margin: 0; font-size: 22px;"">Georgia ERP</h1>
                        <p style=""color: #a9cce3; margin: 4px 0 0; font-size: 13px;"">{Encode(title)}</p>
                    </td>
                </tr>
                <tr>
                    <td style=""padding: 32px;"">
                        {bodyContent}
                    </td>
                </tr>
                <tr>
                    <td style=""background: #f4f6f9; padding: 16px 32px; text-align: center; font-size: 12px; color: #888;"">
                        &copy; {DateTimeOffset.UtcNow.Year} Georgia ERP. All rights reserved.
                    </td>
                </tr>
            </table>
        </td></tr>
    </table>
</body>
</html>";
    }

    private static string Encode(string value) => System.Net.WebUtility.HtmlEncode(value);
}
