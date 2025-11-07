using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Triangulate;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public class EmailSender
    {
        private readonly ILogger<EmailSender> _logger;
        private readonly IConfiguration _config;
        private readonly IEmailBody _emailBody;
        private readonly string _frontendUrl;

        public EmailSender(
            ILogger<EmailSender> logger,
            IConfiguration config,
            IEmailBody emailBody
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

                var emailBody = _emailBody.GenerateRegisterConfirmEmailBody(userName, confirmLink, token);
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
                string confirmLink = $"{_frontendUrl}/confirm-email?email={encodedEmail}&token={encodedToken}";

                var emailBody = _emailBody.GenerateResetPasswordBody(userName, confirmLink, token);
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
    }
}
