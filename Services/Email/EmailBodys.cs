using DTO.Requests;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Text;

namespace Services.Email
{
    public class EmailBodys
    {
        public string GenerateRegisterConfirmEmailBody(string userName, string confirmationLink)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append(".header { background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append(".info-box { background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }");
            sb.Append(".button { display: inline-block; padding: 15px 30px; margin: 20px 0; background-color: #28a745; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }");
            sb.Append(".button:hover { background-color: #218838; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");
            sb.Append("<div class='header'>");
            sb.Append("<h1>Welcome to Fuel App!</h1>");
            sb.Append("</div>");
            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append("<p>Thank you for registering! We're excited to have you on board.</p>");
            sb.Append("<p>To complete your registration and activate your account, please confirm your email address by clicking the button below:</p>");
            sb.Append("<div style='text-align: center;'>");
            sb.Append($"<a href='{confirmationLink}' class='button'>Confirm Email Address</a>");
            sb.Append("</div>");
            sb.Append("<p>If the button doesn't work, copy and paste this link into your browser:</p>");
            sb.Append($"<p style='word-break: break-all; color: #28a745;'>{confirmationLink}</p>");
            sb.Append("<p>If you didn't create an account with us, please ignore this email.</p>");
            sb.Append("<p>Best regards,<br><strong>Fuel App Team</strong></p>");
            sb.Append("</div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p>This is an automated message. Please do not reply to this email.</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");
            return sb.ToString();
        }

        public string GenerateResetPasswordBody(string userName, string confirmationLink)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append(".header { background-color: #007bff; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append(".info-box { background-color: #d1ecf1; border-left: 4px solid #007bff; padding: 15px; margin: 20px 0; }");
            sb.Append(".button { display: inline-block; padding: 15px 30px; margin: 20px 0; background-color: #007bff; color: white; text-decoration: none; border-radius: 5px; font-weight: bold; }");
            sb.Append(".button:hover { background-color: #0056b3; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");
            sb.Append("<div class='header'>");
            sb.Append("<h1>Password Reset Request</h1>");
            sb.Append("</div>");
            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append("<p>We received a request to reset your password. Click the button below to create a new password:</p>");
            sb.Append("<div style='text-align: center;'>");
            sb.Append($"<a href='{confirmationLink}' class='button'>Reset Password</a>");
            sb.Append("<p>If the button doesn't work, copy and paste this link into your browser:</p>");
            sb.Append($"<p style='word-break: break-all; color: #007bff;'>{confirmationLink}</p>");
            sb.Append("<p>Best regards,<br><strong>Fuel App Security Team</strong></p>");
            sb.Append("</div>");
            sb.Append("<div class='footer'>");
            sb.Append("<p>This is an automated message. Please do not reply to this email.</p>");
            sb.Append("</div>");
            sb.Append("</div>");
            sb.Append("</body>");
            sb.Append("</html>");
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

        public string GenerateUnlockEmailBody(string userName, string adminName)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append(".header { background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append(".info-box { background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            sb.Append("<div class='header'>");
            sb.Append("<h1>Account Unlocked</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append("<p>Good news! Your account has been <strong>unlocked</strong> and you can now access our services again.</p>");

            sb.Append("<div class='info-box'>");
            sb.Append("<h3>Unlock Details:</h3>");
            sb.Append("<ul style='list-style: none; padding: 0;'>");
            sb.Append($"<li><strong>Unlocked by:</strong> {adminName}</li>");
            sb.Append($"<li><strong>Date:</strong> {DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm UTC")}</li>");

            sb.Append("</ul>");
            sb.Append("</div>");

            sb.Append("<p>You can now log in and use all features of your account.</p>");
            sb.Append("<p>Please remember to follow our Terms of Service to avoid future account restrictions.</p>");
            sb.Append("<p>If you have any questions, feel free to contact our support team.</p>");
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

        public string GenerateAutoUnlockEmailBody(
            string userName,
            string banReason,
            DateTime bannedAt,
            DateTime bannedUntil)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append(".header { background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append(".info-box { background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            sb.Append("<div class='header'>");
            sb.Append("<h1>Ban Period Has Ended</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append("<p>Your temporary ban has expired and your account has been <strong>automatically unlocked</strong>.</p>");

            sb.Append("<div class='info-box'>");
            sb.Append("<h3>Ban Summary:</h3>");
            sb.Append("<ul style='list-style: none; padding: 0;'>");
            sb.Append($"<li><strong>Original reason:</strong> {banReason}</li>");
            sb.Append($"<li><strong>Banned on:</strong> {bannedAt.ToString("MMMM dd, yyyy")}</li>");
            sb.Append($"<li><strong>Expired on:</strong> {bannedUntil.ToString("MMMM dd, yyyy")}</li>");
            sb.Append($"<li><strong>Unlocked on:</strong> {DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm UTC")}</li>");
            sb.Append("</ul>");
            sb.Append("</div>");

            sb.Append("<p>You can now log in and use all features of your account again.</p>");
            sb.Append("<p><strong>Important:</strong> Please ensure you follow our Terms of Service to avoid future restrictions. Repeated violations may result in longer or permanent bans.</p>");
            sb.Append("<p>If you have any questions or concerns, please contact our support team.</p>");
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

        public string GenerateProposaPriceStatusInfo(string userName, bool isAccepted, FindStationRequest info, decimal newPrice)
        {
            var sb = new StringBuilder();
            sb.Append("<!DOCTYPE html>");
            sb.Append("<html>");
            sb.Append("<head>");
            sb.Append("<meta charset='UTF-8'>");
            sb.Append("<style>");
            sb.Append("body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }");
            sb.Append(".container { max-width: 600px; margin: 0 auto; padding: 20px; background-color: #f9f9f9; border: 1px solid #ddd; border-radius: 8px; }");
            sb.Append(".header { background-color: #28a745; color: white; padding: 20px; text-align: center; border-radius: 8px 8px 0 0; }");
            sb.Append(".content { background-color: white; padding: 30px; border-radius: 0 0 8px 8px; }");
            sb.Append(".info-box { background-color: #d4edda; border-left: 4px solid #28a745; padding: 15px; margin: 20px 0; }");
            sb.Append(".footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }");
            sb.Append("</style>");
            sb.Append("</head>");
            sb.Append("<body>");
            sb.Append("<div class='container'>");

            sb.Append("<div class='header'>");
            sb.Append("<h1>Account Unlocked</h1>");
            sb.Append("</div>");

            sb.Append("<div class='content'>");
            sb.Append($"<p>Dear <strong>{userName}</strong>,</p>");
            sb.Append($"<p>Good news! Your price proposal has been {(isAccepted ? "accepted" : "rejected")} .</p>");
            if (isAccepted)
            {
                sb.Append("<div class='info-box'>");
                sb.Append("<h3>Details Details:</h3>");
                sb.Append("<ul style='list-style: none; padding: 0;'>");
                sb.Append($"<li><strong>Brand:</strong> {info.BrandName}</li>");
                sb.Append($"<li><strong>Street:</strong> {info.Street}</li>");
                sb.Append($"<li><strong>House number:</strong> {info.HouseNumber}</li>");
                sb.Append($"<li><strong>City:</strong> {info.City}</li>");
                sb.Append($"<li><strong>New price:</strong> {newPrice}</li>");
                sb.Append($"<li><strong>Date:</strong> {DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm UTC")}</li>");
                sb.Append("You have +1 point");
                sb.Append("</ul>");
                sb.Append("</div>");
            }

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