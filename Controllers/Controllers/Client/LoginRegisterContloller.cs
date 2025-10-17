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
        /// Authenticate user and return a JWT access token.
        /// </summary>
        /// <remarks>
        /// Description
        /// Authenticates a user using their email and password.
        /// If the credentials are valid, a JWT token is generated and returned.
        /// 
        /// Example request body
        /// ```json
        /// {
        ///   "email": "user@example.pl",
        ///   "password": "User123!"
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
        ///   "expiration": "2025-10-17T12:34:56Z"
        /// }
        /// ```
        ///
        /// Notes
        /// - The returned `token` should be sent in the `Authorization` header as:  
        ///   `Bearer {token}`
        /// - The `expiration` field represents the UTC time when the token becomes invalid.
        /// </remarks>
        /// <response code="200">User successfully logged in</response>
        /// <response code="401">Invalid email or password</response>
        /// <response code="403">User has no assigned roles</response>
        /// <response code="404">User with the given email not found</response>
        /// <response code="500">Server error — something went wrong in the backend</response>

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
