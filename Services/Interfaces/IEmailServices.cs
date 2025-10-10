using Microsoft.AspNetCore.Mvc;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
