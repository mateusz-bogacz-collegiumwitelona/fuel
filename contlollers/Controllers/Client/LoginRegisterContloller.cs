using DTO.Requests;
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
        private readonly ILoginServices _login;
        private readonly IUserServices _userServices;

        public LoginRegisterContloller(
            ILoginServices login,
            IUserServices userServices)
        {
            _login = login;
            _userServices = userServices;
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
        /// <returns>Jwt Token</returns>
        /// <response code="200">User login</response>
        /// <response code="401">Invalid login attempt</response>
        /// <response code="403">User has no roles assigned</response>
        /// <response code="404">Can't find user with email</response>
        /// <response code="500">Something bad in backend. Call 911</response>
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
        [HttpPost("register")]
        public async Task<IActionResult> RegisterNewUserAsync([FromBody] RegisterNewUserRequest request)
        {
            var result = await _userServices.RegisterNewUser(request);
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
