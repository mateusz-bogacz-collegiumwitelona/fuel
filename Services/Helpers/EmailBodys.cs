using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Text;

namespace Services.Helpers
{
    public class EmailBodys : IEmailBody
    {
        public string GenerateRegisterConfirmEmailBody(string userName, string confirmationLink, string token)
        {
            var sb = new StringBuilder();
            sb.Append(token);
            return sb.ToString();
        }

        public string GenerateResetPasswordBody(string userName, string confirmationLink, string token)
        {
            var sb = new StringBuilder();
            sb.Append(token);
            return sb.ToString();
        }

        public string GenerateLockoutEmailBody(string userName, string adminName, int? days, string reason)
        {
            var expiryDate = days.HasValue
                ? DateTime.UtcNow.AddDays(days.Value).ToString("MMMM dd, yyyy 'at' HH:mm UTC")
                : "Never (Permanent)";

            var duration = days.HasValue
                ? $"{days.Value} day{(days.Value > 1 ? "s" : "")}"
                : "Permanent";

            var banType = days.HasValue ? "temporarily suspended" : "permanently banned";
            var headerColor = days.HasValue ? "#dc3545" : "#721c24";
            var headerTitle = days.HasValue ? "Account Suspended" : "Account Permanently Banned";

            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append($".header {{ background-color: {headerColor}; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }}");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append($".info-box {{ background-color: #f8d7da; border-left: 4px solid {headerColor}; padding: 15px; margin: 20px 0; }}");
            sb.Append(".info-box ul { list-style: none; padding: 0; }");
            sb.Append(".info-box li { padding: 5px 0; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            sb.Append("<div class='header'>");
            sb.Append($"<h1>{headerTitle}</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append($"<p>We regret to inform you that your account has been <strong>{banType}</strong> due to a violation of our Terms of Service.</p>");

            sb.Append("<div class='info-box'>");
            sb.Append("<h3>Ban Details:</h3>");
            sb.Append("<ul>");
            sb.Append($"<li><strong>Reason:</strong> {reason}</li>");
            sb.Append($"<li><strong>Duration:</strong> {duration}</li>");

            if (days.HasValue)
            {
                sb.Append($"<li><strong>Ban expires on:</strong> {expiryDate}</li>");
            }

            sb.Append($"<li><strong>Issued by:</strong> {adminName}</li>");
            sb.Append($"<li><strong>Date:</strong> {DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm UTC")}</li>");
            sb.Append("</ul>");
            sb.Append("</div>");

            if (days.HasValue)
            {
                sb.Append("<p>During this period, you will not be able to access your account or use our services.</p>");
            }
            else
            {
                sb.Append("<p>This decision is final and your account will not be reinstated. You will no longer be able to access our services.</p>");
            }

            sb.Append("<p>If you believe this action was taken in error or would like to appeal this decision, please contact our support team at <a href='mailto:support@fuelapp.com'>support@fuelapp.com</a>.</p>");
            sb.Append("<p>We appreciate your understanding and cooperation.</p>");
            sb.Append("<p>Best regards,<br><strong>Fuel App Moderation Team</strong></p>");
            sb.Append("</div>");

            sb.Append("<div class='footer'>");
            sb.Append("<p>This is an automated message. Please do not reply to this email.</p>");
            sb.Append("</div>");

            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");

            return sb.ToString();
        }
    }
}