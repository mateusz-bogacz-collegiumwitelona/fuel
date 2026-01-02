using DTO.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Controllers.Controllers.Client
{
    [Route("api/user")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class UserController : AuthControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly IPriceProposalServices _priceProposalServices;
        private readonly IReportService _reportService;
        public UserController(
            IUserServices userServices,
            IPriceProposalServices priceProposalServices,
            IReportService reportService)
        {
            _userServices = userServices;
            _priceProposalServices = priceProposalServices;
            _reportService = reportService;
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
        [HttpPatch("change-name")]
        public async Task<IActionResult> ChangeUserNameAsync(string userName)
        {
            var (email, error) = GetAuthenticatedUser();
            if (error != null) return error;

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
        [HttpPatch("change-email")]
        public async Task<IActionResult> ChangeUserEmailAsync(
            [Required(ErrorMessage = "Email is required")]
            [EmailAddress(ErrorMessage = "Invalid email format")]
            string newEmail
            )
        {
            var (oldEmail, error) = GetAuthenticatedUser();
            if (error != null) return error;

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
        [HttpPatch("change-password")]
        public async Task<IActionResult> ChangeUserPasswordAsync(ChangePasswordRequest request)
        {
            var (email, error) = GetAuthenticatedUser();
            if (error != null) return error;

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

        /// <summary>
        /// Delete the currently authenticated user's account.
        /// </summary>
        /// <remarks>
        /// Description: Permanently deletes the authenticated user's account. This action cannot be undone. User must provide and confirm their password for verification.
        ///
        /// Example request:
        /// ```http
        /// DELETE /api/user/delete
        /// Content-Type: application/json
        ///
        /// {
        ///   "password": "MyPassword123!",
        ///   "confirmPassword": "MyPassword123!"
        /// }
        /// ```
        ///
        /// Example response (success):
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User deleted successfully."
        /// }
        /// ```
        ///
        /// Example response (error - passwords don't match):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Password and confirm password do not match",
        ///   "errors": ["PasswordMismatch"]
        /// }
        /// ```
        ///
        /// Example response (error - incorrect password):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Current password is incorrect",
        ///   "errors": ["IncorrectCurrentPassword"]
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
        /// - User's email is automatically retrieved from JWT token claims.
        /// - Password and confirm password must match.
        /// - Password verification is required for security.
        /// - This action is permanent and cannot be undone.
        /// - All user data will be removed from the system.
        /// </remarks>
        /// <param name="request">Delete account request containing password and confirmation</param>
        /// <response code="200">Account successfully deleted</response>
        /// <response code="400">Validation error - passwords don't match, incorrect password, or confirm password required</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="404">User with the email doesn't exist</response>
        /// <response code="500">Failed to delete account or unexpected server error</response>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteUserAsyc([FromBody] DeleteAccountRequest request)
        {
            var (email, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _userServices.DeleteUserAsyc(email, request);

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
        /// Report another user for inappropriate behavior or content.
        /// </summary>
        /// <remarks>
        /// **Description:**  
        /// Creates a new user report record in the system.  
        /// The email of the reporting user (notifier) is automatically extracted from the JWT token.  
        /// The report contains the user name of the reported user and a detailed description of the reason.
        ///
        /// **Example request:**
        /// ```http
        /// POST /api/user/report
        ///
        /// {
        ///   "reportedUserName": "user2",
        ///   "reason": "User has been repeatedly sending spam messages in chat rooms for the past few days."
        /// }
        /// ```
        ///
        /// **Example response (success):**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User reported successfully",
        ///   "data": true
        /// }
        /// ```
        ///
        /// **Example response (error - user tries to report themselves):**
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "You cannot report yourself",
        ///   "errors": ["ValidationError"]
        /// }
        /// ```
        ///
        /// **Example response (error - reported user not found):**
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Reported user not found",
        ///   "errors": ["NotFound"]
        /// }
        /// ```
        ///
        /// **Example response (error - unauthorized):**
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "User not authenticated",
        ///   "errors": ["Unauthorized"]
        /// }
        /// ```
        ///
        /// **Notes:**
        /// - The notifier's email is automatically extracted from the JWT token (no need to send it manually).
        /// - The `reason` field must be between **50 and 1000 characters** long and describe the reason for reporting.
        /// - Reports about administrators are automatically rejected.
        /// - A user cannot report themselves.
        /// - The report is stored with the status **Pending** until reviewed by an administrator.
        /// </remarks>
        /// <param name="request">The report request containing the reported user's email and a description of the reason.</param>
        /// <response code="200">Report successfully created.</response>
        /// <response code="400">Validation error — missing fields, invalid email format, or self-report attempt.</response>
        /// <response code="401">User not authenticated or missing JWT token.</response>
        /// <response code="404">Reported user or notifier not found.</response>
        /// <response code="500">Internal server error or database operation failed.</response>

        [HttpPost("report")]
        public async Task<IActionResult> ReportUserAsync([FromBody] ReportRequest request)
        {
            var (notifierEmail, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _reportService.ReportUserAsync(notifierEmail, request);

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
    }
}
