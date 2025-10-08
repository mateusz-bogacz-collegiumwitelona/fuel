using DTO.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace contlollers.Controllers.Client
{
    [ApiController]
    [Route("api/auth")]
    [EnableCors("AllowClient")]
    public class LoginController : ControllerBase
    {
        private readonly ILoginServices _login;
        
        public LoginController(ILoginServices login)
        {
            _login = login;
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginAsync([FromBody] LoginRequest request)
        {
            var result = await _login.HandleLoginAsync(request);

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
