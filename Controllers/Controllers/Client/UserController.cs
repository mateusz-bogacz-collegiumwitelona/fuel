using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Controllers.Controllers.Client
{
    [Route("api/user")]
    [ApiController]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "User,Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public UserController(IUserServices userServices)
        {
            _userServices = userServices;
        }

        /// <summary>
        /// Retrieve information about the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description: Returns detailed information about the authenticated user, including their profile data and proposal statistics.
        /// The user's email is automatically extracted from the JWT token.
        ///
        /// Example request:
        /// ```http
        /// GET /api/user
        /// ```
        ///
        /// Example response:
        /// ```json
        /// {
        ///   "userName": "JohnDoe",
        ///   "email": "user@example.pl",
        ///   "proposalStatistics": {
        ///     "totalProposals": 42,
        ///     "approvedProposals": 30,
        ///     "rejectedProposals": 12,
        ///     "acceptedRate": 71,
        ///     "updatedAt": "2025-10-17T12:34:56Z"
        ///   },
        ///   "createdAt": "2024-01-15T10:30:00Z"
        /// }
        /// ```
        ///
        /// Notes:
        /// - User email is automatically retrieved from JWT token claims.
        /// - Both User and Admin roles have access to this endpoint.
        /// - Returns complete user profile including nested proposal statistics.
        /// - `acceptedRate` in proposal statistics is calculated as percentage of approved proposals.
        /// </remarks>
        /// <response code="200">User information successfully retrieved</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Unexpected server error</response>
        [HttpGet]
        public async Task<IActionResult> GetUserByEmailAsync()
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });

            }
            
            var result = await _userServices.GetUserInfoAsync(email);
            
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
        /// Change the username for the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description: Updates the username for the authenticated user. The user's email is automatically extracted from the JWT token.
        ///
        /// Example request:
        /// ```http
        /// POST /api/user/change-name?userName=NewUsername
        /// ```
        ///
        /// Example response (success):
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "UserName changed successfully.",
        ///   "data": true
        /// }
        /// ```
        ///
        /// Example response (error):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Failed to change UserName.",
        ///   "errors": ["Validation error details"]
        /// }
        /// ```
        ///
        /// Notes:
        /// - User email is automatically retrieved from JWT token claims.
        /// - Both User and Admin roles have access to this endpoint.
        /// - Username must not be empty or whitespace only.
        /// - The normalized username (uppercase) is also updated automatically.
        /// </remarks>
        /// <param name="userName">New username to set for the authenticated user</param>
        /// <response code="200">Username successfully changed</response>
        /// <response code="400">Validation error - userName is null, empty, or whitespace</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="500">Failed to change username or unexpected server error</response>
        [HttpPost("change-name")]
        public async Task<IActionResult> ChangeUserNameAsync(string userName)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {

                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

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

        /// <summary>
        /// Change the email address for the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description: Updates the email address for the authenticated user. The user's current email is automatically extracted from the JWT token.
        ///
        /// Example request:
        /// ```http
        /// POST /api/user/change-email?newEmail=newemail@example.com
        /// ```
        ///
        /// Example response (success):
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Email changed successfully."
        /// }
        /// ```
        ///
        /// Example response (error - email already exists):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with this email newemail@example.com already exists",
        ///   "errors": ["UserAlreadyExist"]
        /// }
        /// ```
        ///
        /// Example response (error - user not found):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User with this email user@example.com doesn't exist",
        ///   "errors": ["UserDoNotExist"]
        /// }
        /// ```
        ///
        /// Notes:
        /// - User's current email is automatically retrieved from JWT token claims.
        /// - New email must be in valid email format.
        /// - New email must be different from the current email.
        /// - The normalized email (uppercase) is also updated automatically.
        /// - Email must not be already registered by another user.
        /// </remarks>
        /// <param name="newEmail">New email address to set for the authenticated user</param>
        /// <response code="200">Email successfully changed</response>
        /// <response code="400">Validation error - email format is invalid or required</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="404">User with the current email doesn't exist</response>
        /// <response code="409">Email already in use by another user</response>
        /// <response code="500">Failed to change email or unexpected server error</response>
        [HttpPost("change-email")]
        public async Task<IActionResult> ChangeUserEmailAsync(
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            string newEmail
            )
        {
            var oldEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(oldEmail))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await _userServices.ChangeUserEmailAsync(oldEmail, newEmail);

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
        /// Change the password for the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description: Updates the password for the authenticated user. The user's email is automatically extracted from the JWT token. User must provide their current password for verification.
        ///
        /// Example request:
        /// ```http
        /// POST /api/user/change-password
        /// Content-Type: application/json
        ///
        /// {
        ///   "currentPassword": "OldPass123!",
        ///   "newPassword": "NewPass456!",
        ///   "confirmNewPassword": "NewPass456!"
        /// }
        /// ```
        ///
        /// Example response (success):
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Password changed successfully."
        /// }
        /// ```
        ///
        /// Example response (error - incorrect current password):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Current password is incorrect",
        ///   "errors": ["IncorrectCurrentPassword"]
        /// }
        /// ```
        ///
        /// Example response (error - passwords do not match):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": ["Passwords do not match"]
        /// }
        /// ```
        ///
        /// Notes:
        /// - User's email is automatically retrieved from JWT token claims.
        /// - Current password must be correct for verification.
        /// - New password must be at least 6 characters long.
        /// - New password must contain at least one uppercase letter, one number, and one special character.
        /// - New password and confirm password must match.
        /// </remarks>
        /// <param name="request">Password change request containing current password, new password, and confirmation</param>
        /// <response code="200">Password successfully changed</response>
        /// <response code="400">Validation error - passwords don't match, incorrect current password, or password requirements not met</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="404">User with the email doesn't exist</response>
        /// <response code="500">Failed to change password or unexpected server error</response>
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangeUserPasswordAsync(ChangePasswordRequest request)
        {
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

            var result = await _userServices.ChangeUserPasswordAsync(email, request);

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
