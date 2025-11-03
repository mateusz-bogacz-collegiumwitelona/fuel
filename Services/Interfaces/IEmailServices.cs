using Microsoft.AspNetCore.Mvc;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IEmailServices
    {
        Task<Result<IActionResult>> SendEmailConfirmationAsync(
            string email,
            string userName,
            string confirmationLink,
            string token
            );
    }
}
