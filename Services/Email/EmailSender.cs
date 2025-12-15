using DTO.Requests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Interfaces;
using System.Net.Mail;

namespace Services.Email
{
    public class EmailSender : IEmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _config;
        private readonly EmailBodys _emailBody;
        private readonly string _frontendUrl;

        public EmailSender(
            ILogger<EmailSender> logger,
            IConfiguration config,
            EmailBodys emailBody
            )
        {
            _logger = logger;
            _config = config;
            _emailBody = emailBody;
            _frontendUrl = _config["Frontend:Url"] ?? "http://localhost:4000";
        }

        private async Task<bool> SendEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var host = _config["Mail:Host"];
                var port = int.Parse(_config["Mail:Port"] ?? "1025");
                var enableSsl = bool.Parse(_config["Mail:EnableSsl"] ?? "false");
                var fromEmail = _config["Mail:From"];
                var displayName = _config["Mail:DisplayName"] ?? "DEV";

                if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(fromEmail))
                {
                    _logger.LogError("Email setting is missing. Check appsettings.json");
                    return false;
                }

                using var message = new MailMessage();
                message.From = new MailAddress(fromEmail, displayName);
                message.To.Add(new MailAddress(toEmail));
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = true;
                message.Priority = MailPriority.High;

                using var smtpClient = new SmtpClient(host, port);

                smtpClient.EnableSsl = enableSsl;
                smtpClient.UseDefaultCredentials = false;
                smtpClient.Credentials = null;
                smtpClient.Timeout = 10000; //10 sek

                await smtpClient.SendMailAsync(message);

                _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject} via {Host}:{Port}",
                    toEmail, subject, host, port);

                return true;
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "SMTP error while sending email to {Email}. Status: {Status}",
                    toEmail, smtpEx.StatusCode);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while sending email to {Email}: {Message}",
                    toEmail, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendRegisterConfirmEmailAsync(
            string email,
            string userName,
            string token
            )
        {
            try
            {
                string encodedToken = Uri.EscapeDataString(token);
                string encodedEmail = Uri.EscapeDataString(email);
                string confirmLink = $"{_frontendUrl}/confirm-email?email={encodedEmail}&token={encodedToken}";

                var emailBody = _emailBody.GenerateRegisterConfirmEmailBody(userName, confirmLink);
                string subject = "Fuel App - Confirm Your Email Address";

                return await SendEmailAsync(email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendRegisterConfirmEmailAsync for {Email}: {Message}",
                    email, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendResetPasswordEmailAsync(
            string email,
            string userName,
            string token
            )
        {
            try
            {
                string encodedToken = Uri.EscapeDataString(token);
                string encodedEmail = Uri.EscapeDataString(email);
                string confirmLink = $"{_frontendUrl}/reset-password?email={encodedEmail}&token={encodedToken}";

                var emailBody = _emailBody.GenerateResetPasswordBody(userName, confirmLink);
                string subject = "Fuel App - Confirm Reset Passowrd";

                return await SendEmailAsync(email, subject, emailBody);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendRegisterConfirmEmailAsync for {Email}: {Message}",
                    email, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendLockoutEmailAsync(
            string email, 
            string userName, 
            string adminName, 
            int? days, 
            string reason)
        {
            try
            {
                var emailBody = _emailBody.GenerateLockoutEmailBody(userName, adminName, days, reason);
                string subject = "Fuel App - Account Lockout Notification";
                var result = await SendEmailAsync(email, subject, emailBody);
                
                if (result)
                {
                    _logger.LogInformation("Lockout email sent successfully to {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Failed to send lockout email to {Email}", email);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendLockoutEmailAsync for {Email}: {Message}",
                    email, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendUnlockEmailAsync(
            string email,
            string userName,
            string adminName
        )
        {
            try
            {
                var emailBody = _emailBody.GenerateUnlockEmailBody(userName, adminName);
                string subject = "Fuel App - Account Unlocked";
                var result = await SendEmailAsync(email, subject, emailBody);

                if (result)
                {
                    _logger.LogInformation("Unlock email sent successfully to {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Failed to send unlock email to {Email}", email);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendUnlockEmailAsync for {Email}: {Message}",
                    email, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendAutoUnlockEmailAsync(
            string email,
            string userName,
            string banReason,
            DateTime bannedAt,
            DateTime bannedUntil)
        {
            try
            {
                var emailBody = _emailBody.GenerateAutoUnlockEmailBody(
                    userName, banReason, bannedAt, bannedUntil);
                string subject = "Fuel App - Your Ban Has Expired";
                var result = await SendEmailAsync(email, subject, emailBody);

                if (result)
                {
                    _logger.LogInformation("Auto-unlock email sent successfully to {Email}", email);
                }
                else
                {
                    _logger.LogWarning("Failed to send auto-unlock email to {Email}", email);
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SendAutoUnlockEmailAsync for {Email}: {Message}",
                    email, ex.Message);
                return false;
            }
        }

        public async Task<bool> SendPriceProposalStatusEmail(
            string email,
            string userName, 
            bool isAccepted, 
            FindStationRequest info, 
            decimal newPrice
            )
        {
            var emailBody = _emailBody.GenerateProposaPriceStatusInfo(userName, isAccepted, info, newPrice);
            string subject = "Fuel App - Price proposal Status";
            var result = await SendEmailAsync(email, subject, emailBody);

            if (result)
            {
                _logger.LogInformation("Email sent successfully to {Email}", email);
            }
            else
            {
                _logger.LogWarning("Failed to send email to {Email}", email);
            }
            return result;
        }
    }
}
