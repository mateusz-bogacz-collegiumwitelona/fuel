using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Internal;
using Services.Interfaces;
using System.Security.Claims;

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
        /// Authenticate user and set a secure HTTP-only cookie with JWT token.
        /// </summary>
        /// <remarks>
        /// Description
        /// Authenticates a user using their email and password.
        /// If the credentials are valid, a JWT token is generated and stored in a secure HTTP-only cookie.
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
        ///   "success": true,
        ///   "message": "Login successful.",
        ///   "data": {
        ///     "message": "Login successful.",
        ///     "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        ///     "email": "user@example.pl",
        ///     "roles": ["User"]
        ///   }
        /// }
        /// ```
        ///
        /// Notes
        /// - The JWT token is automatically stored in a secure HTTP-only cookie named `jwt`
        /// - The cookie is sent automatically with subsequent requests - no manual handling required
        /// - The cookie expires after 3 hours
        /// - For frontend applications, ensure `withCredentials: true` is set in HTTP client (axios/fetch)
        /// - For Swagger/Postman testing, you can still use Bearer token in Authorization header
        /// </remarks>
        /// <response code="200">User successfully logged in, JWT cookie set</response>
        /// <response code="401">Invalid email or password</response>
        /// <response code="403">User has no assigned roles</response>
        /// <response code="404">User with the given email not found</response>
        /// <response code="423">Account locked due to multiple failed login attempts</response>
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
        /// Logout user and clear authentication cookie.
        /// </summary>
        /// <remarks>
        /// Description
        /// Logs out the currently authenticated user by clearing the JWT cookie and signing out from Identity.
        /// 
        /// Example response
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Logout successful."
        /// }
        /// ```
        ///
        /// Notes
        /// - This endpoint requires authentication (must have valid JWT cookie)
        /// - The JWT cookie is removed from the browser
        /// - User session is terminated on the server side
        /// - After logout, protected endpoints will return 401 Unauthorized
        /// </remarks>
        /// <response code="200">User successfully logged out, JWT cookie cleared</response>
        /// <response code="401">User is not authenticated</response>
        /// <response code="500">Server error — something went wrong during logout</response>
        [HttpPost("logout")]
        public async Task <IActionResult> LogoutAsync()
        {
            var result = await _login.LogoutAsync();
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

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshTokenAsync()
        {
            var result = await _login.HandleRefreshAsync();
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserAsync()
        {

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userId) || !Guid.TryParse(userId, out var userGuid))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await _login.GetCurrentUserAsync(userGuid);
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
