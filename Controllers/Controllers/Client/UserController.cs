using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Client
{
    [Route("api/user")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public UserController(IUserServices userServices)
        {
            _userServices = userServices;
        }

        [HttpPost("change-name")]
        public async Task<IActionResult> ChangeUserNameAsync(string email, string userName)
        {
            var result = await _userServices.ChangeUserNameAsync(email, userName);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }
    }
}
