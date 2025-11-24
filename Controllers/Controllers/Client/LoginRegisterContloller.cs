using DTO.Requests;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore.Internal;
using Services.Interfaces;
using System.Security.Claims;

namespace contlollers.Controllers.Client
{
    [ApiController]
    [Route("api/")]
    [EnableCors("AllowClient")]
    [EnableRateLimiting("auth")]
    public class LoginRegisterContloller : ControllerBase
    {
        private readonly ILoginRegisterServices _login;
        private readonly ILogger<LoginRegisterContloller> _logger;
        public LoginRegisterContloller(
            ILoginRegisterServices login,
            ILogger<LoginRegisterContloller> logger
            )
        {
            _login = login;
            logger = _logger;
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

        /// <summary>
        /// Authenticate user via Facebook OAuth and set a secure HTTP-only cookie with JWT token.
        /// </summary>
        /// <remarks>
        /// Description
        /// Authenticates a user using a Facebook access token obtained from Facebook Login SDK.
        /// If the token is valid and the user exists in the system, a JWT token is generated and stored in a secure HTTP-only cookie.
        /// 
        /// Example request body
        /// ```json
        /// {
        ///   "accessToken": "EAABwzLixnjYBO7ZC8ZCqKZBvN9k..."
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Login successful via Facebook",
        ///   "data": {
        ///     "message": "Login successful via Facebook",
        ///     "email": "user@example.pl",
        ///     "userName": "JohnDoe",
        ///     "roles": ["User"]
        ///   }
        /// }
        /// ```
        ///
        /// Notes
        /// - The access token must be obtained from Facebook Login SDK on the client side
        /// - User must be already registered in the system (use /facebook/register for new users)
        /// - The JWT token is automatically stored in a secure HTTP-only cookie named `jwt`
        /// - The cookie is sent automatically with subsequent requests - no manual handling required
        /// - The cookie expires after 3 hours
        /// - For frontend applications, ensure `withCredentials: true` is set in HTTP client (axios/fetch)
        /// - Facebook token is validated against Facebook Graph API
        /// </remarks>
        /// <response code="200">User successfully logged in via Facebook, JWT cookie set</response>
        /// <response code="400">Email not provided by Facebook</response>
        /// <response code="401">Invalid Facebook access token</response>
        /// <response code="404">User not found - registration required</response>
        /// <response code="500">Server error — something went wrong in the backend</response>
        [AllowAnonymous]
        [HttpPost("facebook/login")]
        public async Task<IActionResult> LoginWithFacebookAsync([FromBody] FacebookTokenRequest request)
        {
            var result = await _login.LoginWithFacebookTokenAsync(request.AccessToken, HttpContext);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Register a new user via Facebook OAuth and set a secure HTTP-only cookie with JWT token.
        /// </summary>
        /// <remarks>
        /// Description
        /// Registers a new user using a Facebook access token obtained from Facebook Login SDK.
        /// If the token is valid, a new account is created with email from Facebook profile.
        /// User is automatically assigned the "User" role and logged in.
        /// 
        /// Example request body
        /// ```json
        /// {
        ///   "accessToken": "EAABwzLixnjYBO7ZC8ZCqKZBvN9k..."
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Register successful via Facebook",
        ///   "data": {
        ///     "message": "Register successful via Facebook",
        ///     "email": "newuser@example.pl",
        ///     "userName": "JohnDoe",
        ///     "roles": ["User"]
        ///   }
        /// }
        /// ```
        ///
        /// Notes
        /// - The access token must be obtained from Facebook Login SDK on the client side
        /// - Email must be available in Facebook profile (public permission required)
        /// - If user already exists, registration will fail - use /facebook/login instead
        /// - Username is automatically generated from Facebook name (alphanumeric characters only)
        /// - Email is automatically confirmed (EmailConfirmed = true)
        /// - User is automatically assigned to "User" role
        /// - The JWT token is automatically stored in a secure HTTP-only cookie named `jwt`
        /// - The cookie is sent automatically with subsequent requests - no manual handling required
        /// - The cookie expires after 3 hours
        /// - For frontend applications, ensure `withCredentials: true` is set in HTTP client (axios/fetch)
        /// - Facebook token is validated against Facebook Graph API
        /// </remarks>
        /// <response code="200">User successfully registered via Facebook, JWT cookie set</response>
        /// <response code="400">Email not provided by Facebook</response>
        /// <response code="401">Invalid Facebook access token</response>
        /// <response code="500">Server error — something went wrong in the backend or user creation failed</response>
        [AllowAnonymous]
        [HttpPost("facebook/register")]
        public async Task<IActionResult> RegisterWithFacebookAsync([FromBody] FacebookTokenRequest request)
        {
            var result = await _login.RegisterWithFacebookTokenAsync(request.AccessToken, HttpContext);

            return result.IsSuccess
                ? StatusCode(result.StatusCode, new
                {
                    success = true,
                    message = result.Message,
                    data = result.Data 
                })
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        /// <summary>
        /// Refresh JWT and obtain a new access token.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Generates a new JWT access token and refresh token based on the existing refresh token stored in cookies.  
        /// The existing refresh token will be revoked and replaced with a new one.  
        /// This endpoint does not require authentication via access token, but a valid refresh token cookie is mandatory.
        ///
        /// Example request  
        /// ```http
        /// POST /api/auth/refresh HTTP/1.1
        /// Host: example.com
        /// Cookie: refresh_token=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        /// ```
        ///
        /// Example response (success)  
        /// ```json
        /// {
        ///   "message": "Token refreshed successfully.",
        ///   "email": "john.dope@example.com",
        ///   "userName": "JohnDope",
        ///   "roles": ["User"]
        /// }
        /// ```
        ///
        /// Example response (missing or invalid refresh token)  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Refresh token is missing or invalid",
        ///   "errors": []
        /// }
        /// ```
        ///
        /// Notes  
        /// - Requires a valid refresh token in the `refresh_token` cookie.  
        /// - The old refresh token will be revoked upon successful refresh.  
        /// - Returns a new JWT access token and refresh token.  
        /// - The `X-Token-Expiry` header contains the new JWT expiration date in ISO 8601 format.  
        /// - Will return **401 Unauthorized** if the refresh token is missing, expired, or revoked.  
        /// - Will return **403 Forbidden** if the user has no roles assigned.  
        /// - Will return **404 Not Found** if the user associated with the refresh token does not exist.
        /// </remarks>
        /// <response code="200">Token refreshed successfully — new JWT and refresh token issued</response>
        /// <response code="401">Unauthorized — refresh token missing, expired, or invalid</response>
        /// <response code="403">Forbidden — user has no roles assigned</response>
        /// <response code="404">Not Found — user not found for the provided refresh token</response>
        /// <response code="500">Server error — unexpected exception occurred during refresh</response>
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

        /// <summary>
        /// Retrieve information about the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns detailed information about the user based on the JWT token provided in the `Authorization` header.  
        /// This endpoint requires authentication — a valid bearer token must be included in the request.
        /// 
        /// Example request  
        /// ```http
        /// GET /api/auth/me HTTP/1.1
        /// Host: example.com
        /// Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
        /// ```
        ///
        /// Example response (success)  
        /// ```json
        /// {
        ///   "message": "User retrieved successfully.",
        ///   "email": "john.dope@example.com",
        ///   "userName": "JohnDope",
        ///   "roles": ["User"]
        /// }
        /// ```
        ///
        /// Example response (unauthorized)  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User not authenticated"
        /// }
        /// ```
        ///
        /// Example response (not found)  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User not found.",
        ///   "errors": []
        /// }
        /// ```
        ///
        /// Notes  
        /// - Requires a valid JWT Bearer token in the request header.  
        /// - The token must contain a valid user identifier (`ClaimTypes.NameIdentifier`).  
        /// - Returns user information including username, email, and assigned roles.  
        /// - Will return **401 Unauthorized** if the token is missing or invalid.  
        /// - Will return **404 Not Found** if the user no longer exists in the system.
        /// </remarks>
        /// <response code="200">User retrieved successfully — user info returned</response>
        /// <response code="401">Unauthorized — missing or invalid JWT token</response>
        /// <response code="403">Forbidden — user has no roles assigned</response>
        /// <response code="404">Not Found — user not found in database</response>
        /// <response code="500">Server error — unexpected exception occurred</response>
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
