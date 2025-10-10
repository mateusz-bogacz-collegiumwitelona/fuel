using Microsoft.AspNetCore.Identity;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Helpers
{
    public class EmailBodys : IEmaliBody
    {
        public string GenerateConfirmEmailBody(string userName, string confirmationLink, string token)
        {
            var sb = new StringBuilder();
            sb.Append("<h1>Email Confirmation</h1>");
            sb.Append($"<p>Hi {userName},</p>");
            sb.Append("<p>Thank you for registering. Please confirm your email by clicking the link below:</p>");
            sb.Append($"<a href='{confirmationLink}'>Confirm Email</a>");
            sb.Append($"Token: {token}");
            sb.Append("<p>If you did not register, please ignore this email.</p>");
            sb.Append("<br/>");
            sb.Append("<p>Best regards,<br/>The Fuel App Team</p>");
            return sb.ToString();
        }
    }
}
