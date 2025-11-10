using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Helpers;
using System.Security.Claims;

namespace Controllers.Controllers
{
    public abstract class AuthControllerBase : ControllerBase
    {
        protected string? UserEmail => User.FindFirst(ClaimTypes.Email)?.Value;

        protected (string email, IActionResult? error) GetAuthenticatedUser()
        {
            if (string.IsNullOrEmpty(UserEmail))
            {
                var errorResult = Result<IdentityResult>.Bad(
                    "Unauthenticated. Can't find your data",
                    StatusCodes.Status401Unauthorized,
                    new List<string> { "UnAuthenticated." }
                );

                return (string.Empty, StatusCode(errorResult.StatusCode, new
                {
                    success = false,
                    message = errorResult.Message,
                    errors = errorResult.Errors
                }));
            }

            return (UserEmail!, null);
        }
    }
}
