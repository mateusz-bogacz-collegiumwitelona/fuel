using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class EmailServices : IEmailServices
    {
        private readonly IConfiguration _config;
        private readonly IEmaliBody _emailBodys;
        public EmailServices (
            IConfiguration config,
            IEmaliBody emaliBody)
        {
            _config = config;
            _emailBodys = emaliBody;
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
                return Result<IActionResult>.Bad(
                    "Failed to send email",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );

            }
        }
    }
}
