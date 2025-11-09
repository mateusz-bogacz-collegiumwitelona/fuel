using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Helpers;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace Controllers.Controllers.Client
{
    [Route("api/user")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class UserController : ControllerBase
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
        /// Retrieve information about the currently authenticated user.
        /// </summary>
        /// <remarks>
        /// Description: Returns detailed information about the authenticated user, including their profile data and proposal statistics.
        /// The user's email is automatically extracted from the JWT token.
        ///
        /// Example request:
        /// ```http
        /// GET /api/user/me
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
        [HttpGet("me")]
        public async Task<IActionResult> GetCurrentUserAsync()
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
        /// Retrieve information about the another user.
        /// </summary>
        /// <remarks>
        /// Description: Returns detailed information about another registred user, including their profile data and proposal statistics.
        ///
        /// Example request:
        /// ```http
        /// GET api/user/user2%40example.pl
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
        /// - Both User and Admin roles have access to this endpoint.
        /// - Returns complete user profile including nested proposal statistics.
        /// - `acceptedRate` in proposal statistics is calculated as percentage of approved proposals.
        /// </remarks>
        /// <response code="200">User information successfully retrieved</response>
        /// <response code="401">User not authenticated or email not found in token</response>
        /// <response code="404">User not found</response>
        /// <response code="500">Unexpected server error</response>
        [HttpGet("{email}")]
        public async Task<IActionResult> GetUserByEmailAsync([FromRoute] string email)
        {
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
        /// Get all price proposals submitted by a specific user
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of all price proposals submitted by a user identified by email address.
        /// This endpoint may require admin privileges depending on your authorization configuration.
        /// 
        /// Example request:
        /// <code>
        /// GET /api/user/user@example.pl/price-proposal?PageNumber=3&amp;PageSize=3
        /// </code>
        /// 
        /// Example response:
        /// <code>
        /// {
        ///   "items": [
        ///     {
        ///       "brandName": "Shell",
        ///       "street": "aleja Aleksandra Brücknera",
        ///       "houseNumber": "53",
        ///       "city": "Wrocław",
        ///       "fuelName": "PB95",
        ///       "fuelCode": "PB95",
        ///       "proposedPrice": 4.16,
        ///       "status": "Pending",
        ///       "createdAt": "2025-11-08T18:04:10.789819Z"
        ///     },
        ///     {
        ///       "brandName": "LPG",
        ///       "street": "Stanisławowska",
        ///       "houseNumber": "26",
        ///       "city": "Brzóze",
        ///       "fuelName": "ON",
        ///       "fuelCode": "ON",
        ///       "proposedPrice": 5.65,
        ///       "status": "Pending",
        ///       "createdAt": "2025-11-08T18:04:10.842193Z"
        ///     },
        ///     {
        ///       "brandName": "Orlen",
        ///       "street": "Danuty Siedzikówny \"Inki\"",
        ///       "houseNumber": "16",
        ///       "city": "Iłża",
        ///       "fuelName": "PB95",
        ///       "fuelCode": "PB95",
        ///       "proposedPrice": 5.01,
        ///       "status": "Pending",
        ///       "createdAt": "2025-11-08T18:04:10.866675Z"
        ///     }
        ///   ],
        ///   "pageNumber": 3,
        ///   "pageSize": 3,
        ///   "totalCount": 9,
        ///   "totalPages": 3,
        ///   "hasPreviousPage": true,
        ///   "hasNextPage": false
        /// }
        /// </code>
        /// 
        /// Notes:
        /// - Email parameter must be URL-encoded (e.g., user@example.pl becomes user%40example.pl)
        /// - Returns all proposals regardless of status (Pending, Approved, Rejected)
        /// - Default pagination: PageNumber=1, PageSize=10
        /// - If PageNumber exceeds totalPages, the last page is returned automatically
        /// - Returns empty items array if user has not submitted any proposals
        /// </remarks>
        /// <param name="email">Email address of the user whose proposals to retrieve</param>
        /// <response code="200">Price proposals retrieved successfully (may be empty list)</response>
        /// <response code="400">Invalid email or user not found</response>
        /// <response code="401">Unauthorized - invalid or missing authentication</response>
        /// <response code="500">Server error - something went wrong while processing the request</response>
        [HttpGet("{email}/price-proposal")]
        public async Task<IActionResult> GetUserPriceProposals([FromRoute] string email, [FromQuery] GetPaggedRequest pagged)
        {
            var result = await _priceProposalServices.GetUserAllProposalPricesAsync(email, pagged);
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
        /// Get all price proposals submitted by the currently authenticated user
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of all price proposals that the authenticated user has submitted.
        /// Requires authentication.
        /// 
        /// Example request:
        /// <code>
        /// GET /api/user/me/price-proposal?PageNumber=1&amp;PageSize=10
        /// </code>
        /// 
        /// Example response:
        /// <code>
        /// {
        ///   "items": [
        ///     {
        ///       "brandName": "Orlen",
        ///       "street": "Piotra Mocka",
        ///       "houseNumber": "3",
        ///       "city": "Mosina",
        ///       "fuelName": "ON",
        ///       "fuelCode": "ON",
        ///       "proposedPrice": 4.05,
        ///       "status": "Pending",
        ///       "createdAt": "2025-11-08T18:04:10.198868Z"
        ///     },
        ///     {
        ///       "brandName": "Shell",
        ///       "street": "Jana Pawła II",
        ///       "houseNumber": "12",
        ///       "city": "Poznań",
        ///       "fuelName": "PB98",
        ///       "fuelCode": "PB98",
        ///       "proposedPrice": 4.06,
        ///       "status": "Pending",
        ///       "createdAt": "2025-11-08T18:04:10.592618Z"
        ///     }...
        ///   ],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 9,
        ///   "totalPages": 1,
        ///   "hasPreviousPage": false,
        ///   "hasNextPage": false
        /// }
        /// </code>
        /// 
        /// Notes:
        /// - Requires user to be authenticated (valid JWT token)
        /// - Returns all proposals regardless of status (Pending, Approved, Rejected)
        /// - Default pagination: PageNumber=1, PageSize=10
        /// - If PageNumber exceeds totalPages, the last page is returned automatically
        /// - Returns empty items array if user has not submitted any proposals
        /// </remarks>
        /// <response code="200">Price proposals retrieved successfully (may be empty list)</response>
        /// <response code="401">User not authenticated or invalid token</response>
        /// <response code="500">Server error - something went wrong while processing the request</response>
        [HttpGet("me/price-proposal")]
        public async Task<IActionResult> GetUserPriceProposals([FromQuery] GetPaggedRequest pagged)
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

            var result = await _priceProposalServices.GetUserAllProposalPricesAsync(email, pagged);
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
        [HttpPatch("change-name")]
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
        [HttpPatch("change-email")]
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
        /// - Both User and Admin roles have access to this endpoint.
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
            var email = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(email))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

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
        /// The report contains the email of the reported user and a detailed description of the reason.
        ///
        /// **Example request:**
        /// ```http
        /// POST /api/user/report
        ///
        /// {
        ///   "reportedEmail": "user2@example.pl",
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
        /// - The `reportedEmail` field must contain a valid, existing user email address.
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
            var notifierEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(notifierEmail))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });
            }

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
