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
    }
}