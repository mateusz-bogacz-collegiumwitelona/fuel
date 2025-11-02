using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Services.Interfaces;

namespace contlollers.Controllers.Client
{
    [ApiController]
    [Route("api/")]
    [EnableCors("AllowClient")]
    public class LoginRegisterContloller : ControllerBase
    {
        private readonly ILoginRegisterServices _login;
        public LoginRegisterContloller(
            ILoginRegisterServices login
            )
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
        /// Example request body for user
        /// ```json
        /// {
        ///   "email": "user@example.pl",
        ///   "password": "User123!"
        /// }
        /// ```
        /// 
        /// Example request body for admin
        /// ```json
        /// {
        ///   "email": "admin@example.pl",
        ///   "password": "Admin123!"
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
        /// Register a new user account.
        /// </summary>
        /// <remarks>
        /// Description
        /// Creates a new user account with the provided username, email, and password.
        /// After successful registration, a confirmation email is sent to the user's email address.
        /// 
        /// Example request body
        /// ```json
        /// {
        ///   "userName": "JohnDope",
        ///   "email": "john.dope@example.com",
        ///   "password": "John!23",
        ///   "confirmPassword": "John!23"
        /// }
        /// ```
        ///
        /// Example response (success)
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User registered successfully. Please check your email to confirm your account."
        /// }
        /// ```
        ///
        /// Example response (error)
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with this email: john.dope@example.com already exists",
        ///   "errors": []
        /// }
        /// ```
        ///
        /// Notes
        /// - Password must be at least 6 characters long and contain:
        ///   - At least one uppercase letter
        ///   - At least one number
        ///   - At least one special character
        /// - A confirmation email will be sent to the provided email address
        /// - The user must confirm their email before they can log in
        /// - Username and email must be unique
        /// </remarks>
        /// <response code="201">User registered successfully — confirmation email sent</response>
        /// <response code="400">Validation errors — email/username already exists or invalid input</response>
        /// <response code="500">Server error — something went wrong in the backend</response>
        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<IActionResult> RegisterNewUserAsync([FromBody] RegisterNewUserRequest request)
        {
            var result = await _login.RegisterNewUserAsync(request);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Confirm user email address
        /// </summary>
        /// <remarks>
        /// Description:
        /// Confirms a user's email address using the token sent via email during registration.
        /// 
        /// Example request:
        /// ```json
        /// {
        ///   "email": "john.dope@example.com",
        ///   "token": "CfDJ8Abc123..."
        /// }
        /// ```
        ///
        /// Example response (success):
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Email confirmed successfully. You can now log in."
        /// }
        /// ```
        ///
        /// Example response (error):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Invalid or expired confirmation token",
        ///   "errors": ["Invalid token"]
        /// }
        /// ```
        ///
        /// Notes:
        /// - The token is sent to the user's email during registration
        /// - Tokens typically expire after 24 hours
        /// - Once confirmed, the user can log in to the application
        /// - If email is already confirmed, a 400 error will be returned
        /// </remarks>
        /// <response code="200">Email confirmed successfully</response>
        /// <response code="400">Invalid or expired token, or email already confirmed</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Server error</response>
        [AllowAnonymous]
        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmailAsync([FromBody] ConfirmEmailRequest request)
        {
            var result = await _login.ConfirmEmailAsync(request);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Request password reset - sends an email with reset token
        /// </summary>
        /// <param name="email">User's email address</param>
        /// <returns>Result indicating if password reset email was sent successfully</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/reset-password?email=user@example.com
        ///     
        /// This endpoint:
        /// - Validates if the user exists
        /// - Checks if email is confirmed
        /// - Generates a password reset token
        /// - Sends an email with reset instructions
        /// 
        /// The token expires after 24 hours.
        /// </remarks>
        /// <response code="200">Password reset email sent successfully</response>
        /// <response code="400">Email is not confirmed</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Internal server error</response>
        [AllowAnonymous]
        [HttpPost("reset-password")]
        public async Task<IActionResult> ForgotPasswordAsync(string email)
        {
            var result = await _login.ForgotPasswordAsync(email);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Set a new password using reset token
        /// </summary>
        /// <param name="request">Password reset details including email, token, and new password</param>
        /// <returns>Result indicating if password was reset successfully</returns>
        /// <remarks>
        /// Sample request:
        /// 
        ///     POST /api/set-new-password
        ///     {
        ///         "email": "user@example.com",
        ///         "token": "CfDJ8IdAXN6s0V1Cl4t834jHBx...",
        ///         "password": "NewSecure123!",
        ///         "confirmPassword": "NewSecure123!"
        ///     }
        ///     
        /// Password requirements:
        /// - Minimum 6 characters
        /// - At least one uppercase letter (A-Z)
        /// - At least one number (0-9)
        /// - At least one special character (!@#$%^&amp;*(),.?":{}|&lt;&gt;)
        /// 
        /// The token must be the one received via email from the reset-password endpoint.
        /// Token expires after 24 hours.
        /// </remarks>
        [AllowAnonymous]
        [HttpPost("set-new-password")]
        public async Task<IActionResult> SetNewPassowrdAsync([FromBody] ResetPasswordRequest request)
        {
            var result = await _login.SetNewPassowrdAsync(request);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }
    }
}
