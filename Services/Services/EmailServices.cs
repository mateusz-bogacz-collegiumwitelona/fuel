using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;
using System.Net.Mail;

namespace Services.Services
{
    public class EmailServices : IEmailServices
    {
        private readonly IConfiguration _config;
        private readonly IEmaliBody _emailBodys;
        private readonly ILogger<EmailServices> _logger;

        public EmailServices (
            IConfiguration config,
            IEmaliBody emaliBody,
            ILogger<EmailServices> logger)
        {
            _config = config;
            _emailBodys = emaliBody;
            _logger = logger;
        }

        public async Task<Result<IActionResult>> SendEmailConfirmationAsync(
            string email,
            string userName,
            string confirmationLink,
            string token
            )
        {
            try
            {
                var smtpHost = _config["Mail:Host"] ?? "mailpit";
                var smtpPort = int.Parse(_config["Mail:Port"] ?? "1025");

                using var smtpClient = new SmtpClient(smtpHost, smtpPort)
                {
                    EnableSsl = false,
                    UseDefaultCredentials = false,
                    Credentials = null
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress("noreplay@fuel.com", "Fuel APP"),
                    Subject = "Confirm your email",
                    Body = _emailBodys.GenerateConfirmEmailBody(userName, confirmationLink, token),
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(email);
                await smtpClient.SendMailAsync(mailMessage);    

                return Result<IActionResult>.Good(
                    "Email sent successfully",
                    StatusCodes.Status200OK,
                    new OkResult()
                    );

            }            
            catch (Exception ex)
            {
                _logger.LogError($"Error sending email to {email}: {ex.Message} | {ex.InnerException} ");

                return Result<IActionResult>.Bad(
                    "Failed to send email",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" }
                    );

            }
        }
    }
}
