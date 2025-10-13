using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Services;

namespace contlollers.Controllers.Client
{
    [ApiController]
    [Route("api/")]
    [EnableCors("AllowClient")]
    public class LoginRegisterContloller : ControllerBase
    {
        private readonly ILoginRegisterServices _login;

        public LoginRegisterContloller(ILoginRegisterServices login)
        {
            _login = login;
        }

        /// <summary>
        /// Login user and return JWT token
        /// </summary>
        /// <param name="request">
        /// DTO with email and password.
        /// <br/><br/>
        /// <b>Example request:</b>
        /// <br/>
        /// {
        ///     "email": "user@example.pl",
        ///     "password": "User123!"
        /// }
        /// </param>
        /// <remarks>
        /// {
        ///   "token": "string",
        ///   "expiration": "DateTime"
        /// }
        /// </remarks>
        /// <response code="200">User login</response>
        /// <response code="401">Invalid login attempt</response>
        /// <response code="403">User has no roles assigned</response>
        /// <response code="404">Can't find user with email</response>
        /// <response code="500">Something bad in backend. Call 911</response>
        [AllowAnonymous]
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

        /// <summary>
        /// Register new user
        /// </summary>
        /// <param name="request">
        /// DTO with UserName, Email, Password and ConfirmPassword 
        /// <br/><br/>
        /// <b>Example request:</b>
        /// <br/>
        /// {
        /// "userName": "JohnDope",
        /// "email": "john.dope@example.com",
        /// "password": "John!23",
        /// "confirmPassword": "John!23"
        /// }
        /// </param>
        /// <returns>IdentityResult with messages</returns>
        /// <response code="400">Validation Errors or Error with repo</response>
        /// <response code="201">Success</response>
        /// <response code="500">Something bad in backend. Call priest or Dev</response>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterNewUserAsync([FromBody] RegisterNewUserRequest request)
        {
            var result = await _login.RegisterNewUser(request);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Make email confirmation for user
        /// </summary>
        /// <param name="email"></param>
        /// <param name="token"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequest request)
        {
            var result = await _login.ConfirmEmailAsync(request);
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
