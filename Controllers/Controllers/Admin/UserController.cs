using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/user")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class UserController : AuthControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly IBanService _banService;
        private readonly IReportService _reportService;
        public UserController(
            IUserServices userServices,
            IBanService banService,
            IReportService reportService)
        {
            _userServices = userServices;
            _banService = banService;
            _reportService = reportService;
        }


        /// <summary>
        /// Retrieves a paginated and filterable list of users
        /// </summary>
        /// <remarks>
        /// Returns a complete list of users (non-deleted) with their assigned roles, creation dates, and ban status.
        /// Supports searching, sorting, and pagination for admin management purposes.
        /// 
        /// **Features:**
        /// - Filter users by username, email, or role name
        /// - Sort by username, email, role, creation date, or ban status
        /// - Paginate results using `PageNumber` and `PageSize`
        /// - Automatically displays ban status for each user
        /// 
        /// **Sample Request:**
        /// 
        ///     GET /api/admin/user/list?Search=user&amp;SortBy=roles&amp;SortDirection=asc&amp;PageNumber=1&amp;PageSize=10
        /// 
        /// **Sample Response:**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "Users retrieved successfully",
        ///       "data": {
        ///         "items": [
        ///           {
        ///             "userName": "Admin",
        ///             "email": "admin@example.pl",
        ///             "roles": "Admin",
        ///             "createdAt": "2025-11-07T14:38:54.705875Z",
        ///             "isBanned": false
        ///           },
        ///           {
        ///             "userName": "User1",
        ///             "email": "user1@example.pl",
        ///             "roles": "User",
        ///             "createdAt": "2025-11-07T14:38:55.153691Z",
        ///             "isBanned": true
        ///           }
        ///         ],
        ///         "pageNumber": 1,
        ///         "pageSize": 10,
        ///         "totalCount": 2,
        ///         "totalPages": 1,
        ///         "hasPreviousPage": false,
        ///         "hasNextPage": false
        ///       }
        ///     }
        /// 
        /// 
        /// **Supported Sorting Fields:**
        /// - `username` → alphabetical order
        /// - `email` → alphabetical order
        /// - `roles` or `role` → by role priority (Admin > User)
        /// - `createdAt` or `created` → by account creation date
        /// - `isBanned`, `banned`, or `ban` → banned users first/last (with secondary sort by username)
        /// 
        /// **Default Sorting:**
        /// - When no `SortBy` is specified, users are sorted by role priority (Admin first) then by username
        /// 
        /// **Pagination Behavior:**
        /// - If requested page number exceeds total pages, automatically returns the last available page
        /// - Returns empty result set with appropriate metadata if no users are found
        /// - Page numbers and sizes default to 1 and 10 respectively if not specified
        /// 
        /// </remarks>
        /// <param name="pagged">Pagination configuration (page number and page size)</param>
        /// <param name="request">Search and sorting configuration (search text, sort field, sort direction)</param>
        /// <returns>Paginated list of users with role, ban status, and account details</returns>
        /// <response code="200">Users retrieved successfully (including empty result sets)</response>
        /// <response code="400">Invalid query parameters</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>

        [HttpGet("list")]
        public async Task<IActionResult> GetUserListAsync(
            [FromQuery] GetPaggedRequest pagged,
            [FromQuery] TableRequest request)
        {
            var result = await _userServices.GetUserListAsync(pagged, request);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors,
                    Data = result.Data
                });
        }

        /// <summary>
        /// Changes the role of an existing user.
        /// </summary>
        /// <remarks>
        /// Allows an administrator to promote or demote a user between available roles.
        /// Currently, the system supports two roles: <c>User</c> and <c>Admin</c>.
        /// 
        /// When changing a role:
        /// - Promoting a user to <c>Admin</c> automatically removes the <c>User</c> role.
        /// - Demoting an admin to <c>User</c> automatically removes the <c>Admin</c> role.
        /// 
        /// **Sample request:**
        /// 
        ///     PUT /api/admin/user/change-role?email=user@example.pl&amp;newRole=Admin
        /// 
        /// **Sample success response (200 OK):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "Role changed successfully to Admin.",
        ///       "data": {
        ///         "succeeded": true,
        ///         "errors": []
        ///       }
        ///     }
        /// 
        /// **Sample error response (404 Not Found):**
        /// 
        ///     {
        ///       "success": false,
        ///       "message": "User with this email user@example.pl doesn't exist.",
        ///       "errors": [ "UserDoNotExist" ]
        ///     }
        /// 
        /// **Important Notes:**
        /// - Only users with the <c>Admin</c> role can access this endpoint.
        /// - The <c>email</c> parameter must correspond to a registered user.
        /// - The <c>newRole</c> parameter must be a valid existing role (<c>User</c> or <c>Admin</c>).
        /// 
        /// </remarks>
        /// <param name="email">Email of the user whose role will be changed.</param>
        /// <param name="newRole">Target role to assign (<c>User</c> or <c>Admin</c>).</param>
        /// <returns>Operation result with success flag and identity result details.</returns>
        /// <response code="200">Role changed successfully</response>
        /// <response code="400">Invalid input data or missing parameters</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="404">User or role not found</response>
        /// <response code="500">Internal server error</response>

        [HttpPatch("change-role")]
        public async Task<IActionResult> ChangeUserRoleAsync([FromQuery] string email, [FromQuery] string newRole)
        {
            var result = await _userServices.ChangeUserRoleAsync(email, newRole);
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
                    errors = result.Errors,
                    data = result.Data
                });
        }

        /// <summary>
        /// Bans or suspends a user account with specified reason and duration
        /// </summary>
        /// <remarks>
        /// Applies a temporary or permanent ban to a user account, preventing them from accessing the application.
        /// Automatically deactivates any previous active bans, creates a new ban record, and sends a notification email to the affected user.
        /// 
        /// **Features:**
        /// - Temporary ban (specify number of days) or permanent ban (omit days)
        /// - Automatic deactivation of previous active bans before applying new one
        /// - Email notification sent to banned user with ban details
        /// - Complete ban audit trail with admin information
        /// - Protection against banning admin accounts
        /// - After giving ban for user his report recor has been clear (status = Accepted)
        /// 
        /// **Ban Types:**
        /// - **Temporary Ban**: Specify `days` parameter (e.g., 7, 30, 90)
        /// - **Permanent Ban**: Leave `days` as null or omit it
        /// 
        /// **Sample Request (Temporary Ban):**
        /// 
        ///     POST /api/admin/lock-out
        ///     {
        ///       "email": "user@example.pl",
        ///       "reason": "Violation of Terms of Service - inappropriate content",
        ///       "days": 7
        ///     }
        /// 
        /// **Sample Request (Permanent Ban):**
        /// 
        ///     POST /api/admin/lock-out
        ///     {
        ///       "email": "user@example.pl",
        ///       "reason": "Severe violation - repeated offenses",
        ///       "days": null
        ///     }
        /// 
        /// **Sample Response (Success):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "User banned successfully for 7 days",
        ///       "data": {
        ///         "succeeded": true,
        ///         "errors": []
        ///       }
        ///     }
        /// 
        /// **Sample Response (Permanent Ban Success):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "User banned permanently",
        ///       "data": {
        ///         "succeeded": true,
        ///         "errors": []
        ///       }
        ///     }
        /// 
        /// </remarks>
        /// <param name="request">Ban configuration (user email, reason, optional duration in days)</param>
        /// <returns>Ban operation result with success status and details</returns>
        /// <response code="200">User banned successfully (temporary or permanent)</response>
        /// <response code="400">Invalid request - email or reason missing</response>
        /// <response code="401">Unauthorized - valid JWT token required or admin email not found in claims</response>
        /// <response code="403">Forbidden - Cannot ban admin accounts or requesting user is not an admin</response>
        /// <response code="404">User not found or admin not found</response>
        /// <response code="500">Internal server error - failed to apply ban or create ban record</response>
        [HttpPost("lock-out")]
        public async Task<IActionResult> LockoutUserAsync(SetLockoutForUserRequest request)
        {
            var (adminEmail, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _banService.LockoutUserAsync(adminEmail, request);
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
                    errors = result.Errors,
                    data = result.Data
                });
        }

        /// <summary>
        /// Retrieves detailed information about a user's active ban
        /// </summary>
        /// <remarks>
        /// Returns comprehensive details about a user's current active ban status, including the reason, duration, and admin who issued the ban.
        /// Only retrieves information for currently active bans - returns 404 if user is not banned or has no active ban records.
        /// 
        /// **Features:**
        /// - View complete ban details for any user
        /// - Displays ban reason and justification
        /// - Shows ban duration (temporary or permanent)
        /// - Identifies admin who issued the ban
        /// - Provides timestamps for ban start and expiry
        /// 
        /// **Sample Request:**
        /// 
        ///     GET /api/admin/user/lock-out/review?email=user@example.pl
        /// 
        /// **Sample Response (Active Ban - Temporary):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "Ban information retrieved successfully.",
        ///       "data": {
        ///         "userName": "User2",
        ///         "reason": "Violation of Terms of Service - inappropriate content",
        ///         "bannedAt": "2025-11-07T22:08:57.51804Z",
        ///         "bannedUntil": "2025-11-14T22:08:57.518078Z",
        ///         "bannedBy": "Admin"
        ///       }
        ///     }
        /// 
        /// **Sample Response (Active Ban - Permanent):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "Ban information retrieved successfully.",
        ///       "data": {
        ///         "userName": "User2",
        ///         "reason": "Severe violation - repeated offenses",
        ///         "bannedAt": "2025-11-07T22:08:57.51804Z",
        ///         "bannedUntil": "9999-12-31T23:59:59.9999999Z",
        ///         "bannedBy": "Admin"
        ///       }
        ///     }
        /// 
        /// **Sample Response (No Active Ban):**
        /// 
        ///     {
        ///       "success": false,
        ///       "message": "No ban information found for the user.",
        ///       "errors": ["NoBanInfoFound"],
        ///       "data": null
        ///     }
        /// 
        /// **Response Fields Explained:**
        /// - `userName` - Username of the banned user
        /// - `reason` - Detailed reason for the ban as provided by admin
        /// - `bannedAt` - UTC timestamp when ban was applied
        /// - `bannedUntil` - UTC timestamp when ban expires (or DateTime.MaxValue for permanent bans)
        /// - `bannedBy` - Username of the admin who issued the ban
        /// 
        /// **Ban Type Identification:**
        /// - **Temporary Ban**: `bannedUntil` contains a realistic future date
        /// - **Permanent Ban**: `bannedUntil` is set to DateTime.MaxValue (9999-12-31T23:59:59.9999999Z)
        /// 
        /// </remarks>
        /// <param name="email">Email address of the user to check ban status (query parameter)</param>
        /// <returns>Detailed information about the user's active ban</returns>
        /// <response code="200">Ban information retrieved successfully</response>
        /// <response code="401">Unauthorized - email parameter is missing or empty</response>
        /// <response code="404">No active ban found for the specified user</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("lock-out/review")]
        public async Task<IActionResult> ReviewLockoutAsync([FromQuery] string email)
        {

            var result = await _banService.GetUserBanInfoAsync(email);
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
                    errors = result.Errors,
                    data = result.Data
                });
        }

        /// <summary>
        /// Unlocks a banned or suspended user account
        /// </summary>
        /// <remarks>
        /// Removes an active ban from a user account, restoring their access to the application.
        /// Automatically deactivates all active ban records, resets failed login attempts, and sends a notification email to the user.
        /// 
        /// **Features:**
        /// - Removes temporary or permanent bans
        /// - Automatic deactivation of all active ban records with audit trail
        /// - Resets failed access attempt counter
        /// - Email notification sent to unbanned user
        /// - Complete unlock audit trail with admin information
        /// 
        /// **Sample Request:**
        /// 
        ///     POST /api/admin/unlock?userEmail=user@example.pl
        /// 
        /// **Sample Response (Success):**
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "User unlocked successfully",
        ///       "data": {
        ///         "succeeded": true,
        ///         "errors": []
        ///       }
        ///     }
        /// 
        /// **Sample Response (User Not Banned):**
        /// 
        ///     {
        ///       "success": false,
        ///       "message": "User is not locked out.",
        ///       "errors": ["UserNotLockedOut"],
        ///       "data": null
        ///     }
        /// 
        /// </remarks>
        /// <param name="userEmail">Email address of the user to unlock (query parameter)</param>
        /// <returns>Unlock operation result with success status and details</returns>
        /// <response code="200">User unlocked successfully</response>
        /// <response code="400">Invalid request - email missing or user is not currently locked out</response>
        /// <response code="401">Unauthorized - valid JWT token required or admin email not found in claims</response>
        /// <response code="403">Forbidden - Requesting user is not an admin (cannot unlock users) or attempting to unlock an admin account</response>
        /// <response code="404">User not found or admin not found</response>
        /// <response code="500">Internal server error - failed to unlock user or update ban records</response>
        [HttpPost("unlock")]
        public async Task<IActionResult> UnlockUserAsync([FromQuery] string userEmail)
        {
            var (adminEmail, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _banService.UnlockUserAsync(adminEmail, userEmail);
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
                    errors = result.Errors,
                    data = result.Data
                });
        }

        /// <summary>
        /// Retrieves a paginated list of pending user reports for a specific user
        /// </summary>
        /// <param name="email">Email address of the reported user</param>
        /// <param name="pagged">Pagination parameters (PageNumber and PageSize)</param>
        /// <returns>A paginated list of pending reports for the specified user</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/report/list?email=user@example.com&amp;PageNumber=1&amp;PageSize=10
        ///
        /// This endpoint returns only reports with **"Pending"** status.
        /// Reports are ordered by creation date (oldest first).
        /// 
        /// **Query Parameters:**
        /// - `email` (required): Email of the reported user whose reports you want to retrieve
        /// - `PageNumber` (optional): Page number to retrieve (default: 1, minimum: 1)
        /// - `PageSize` (optional): Number of items per page (default: 10, minimum: 1)
        /// 
        /// **Response Structure:**
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "User reports retrieved successfully",
        ///   "data": {
        ///     "items": [
        ///       {
        ///         "userName": "reportedUser123",
        ///         "userEmail": "reported@example.com",
        ///         "reason": "Spam or inappropriate content",
        ///         "status": "Pending",
        ///         "createdAt": "2025-11-08T10:30:00Z"
        ///       }
        ///     ],
        ///     "pageNumber": 1,
        ///     "pageSize": 10,
        ///     "totalCount": 15,
        ///     "totalPages": 2,
        ///     "hasPreviousPage": false,
        ///     "hasNextPage": true
        ///   }
        /// }
        /// ```
        /// 
        /// **Error Response Example:**
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Reported user not found",
        ///   "errors": ["NotFound"],
        ///   "data": null
        /// }
        /// ```
        /// 
        /// **Notes:**
        /// - Only reports with "Pending" status are returned
        /// - If the requested page number exceeds total pages, the last page is returned automatically
        /// - An empty result (no reports) returns a 200 status with an empty items array
        /// </remarks>
        /// <response code="200">Returns the paginated list of user reports or empty list if no reports found</response>
        /// <response code="400">If the email parameter is null or empty</response>
        /// <response code="404">If the user with the specified email is not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("report/list")]
        public async Task<IActionResult> GetUserReportAsync(
            [Required][FromQuery] string email,
            [FromQuery] GetPaggedRequest pagged)
        {
            var result = await _reportService.GetUserReportAsync(email, pagged);

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
                    errors = result.Errors,
                    data = result.Data
                });
        }

        /// <summary>
        /// Changes the status of a user report (accept with ban or reject)
        /// </summary>
        /// <param name="request">Report status change details</param>
        /// <returns>Result of the status change operation</returns>
        /// <remarks>
        /// Sample request for ACCEPTING a report (bans the user):
        ///
        ///     PUT /api/admin/user/report/change-status
        ///     {
        ///       "isAccepted": true,
        ///       "reportedUserEmail": "spammer@example.com",
        ///       "reportingUserEmail": "victim@example.com",
        ///       "reportCreatedAt": "2025-11-08T10:30:45.1234567Z",
        ///       "reason": "Multiple spam reports confirmed",
        ///       "days": 7
        ///     }
        ///
        /// Sample request for REJECTING a report:
        ///
        ///     PUT /api/admin/user/report/change-status
        ///     {
        ///       "isAccepted": false,
        ///       "reportedUserEmail": "user@example.com",
        ///       "reportingUserEmail": "reporter@example.com",
        ///       "reportCreatedAt": "2025-11-08T10:30:45.1234567Z"
        ///     }
        ///
        /// **Request Body Fields:**
        /// 
        /// - **`isAccepted`** (required, boolean):
        ///   - `true` = Accept the report and ban the user
        ///   - `false` = Reject the report (no action taken)
        /// 
        /// - **`reportedUserEmail`** (required, string):
        ///   - Email address of the user who was reported
        ///   - Must be a valid email format
        /// 
        /// - **`reportingUserEmail`** (required, string):
        ///   - Email address of the user who made the report
        ///   - Must be a valid email format
        /// 
        /// - **`reportCreatedAt`** (required, datetime):
        ///   - Exact timestamp when the report was created
        ///   - Use the value from the report list endpoint
        ///   - Format: ISO 8601 (e.g., "2025-11-08T10:30:45.1234567Z")
        ///   - This field, combined with emails, uniquely identifies the report
        /// 
        /// - **`reason`** (optional, string):
        ///   - Admin's reason for the ban
        ///   - **REQUIRED when `isAccepted` is `true`**
        ///   - Ignored when `isAccepted` is `false`
        ///   - This will be included in the ban notification email sent to the user
        /// 
        /// - **`days`** (optional, integer):
        ///   - Duration of the ban in days
        ///   - Only applies when `isAccepted` is `true`
        ///   - `null` or not provided = permanent ban
        ///   - Positive number (e.g., 7) = temporary ban for that many days
        ///   - Ignored when `isAccepted` is `false`
        /// 
        /// ---
        /// 
        /// **When accepting (isAccepted: true):**
        /// - ALL pending reports for the reported user automatically become "Accepted"
        /// - Creates a ban record in the database
        /// - Locks out the user's account (sets LockoutEnd in Identity)
        /// - Sends a ban notification email to the reported user
        /// - The `reason` field is **REQUIRED** - validation will fail without it
        /// - All accepted reports will be linked to the created ban record
        /// 
        /// **When rejecting (isAccepted: false):**
        /// - Only THIS specific report's status changes to "Rejected"
        /// - Other pending reports for the same user remain unchanged
        /// - No ban is created
        /// - User account remains active
        /// - The `reason` and `days` fields are ignored
        /// 
        /// **Notes:**
        /// - The combination of `reportedUserEmail`, `reportingUserEmail`, and `reportCreatedAt` uniquely identifies a specific report
        /// - Only reports with "Pending" status can be processed
        /// - Attempting to process an already reviewed report will return a 400 error
        /// - Only users with the "Admin" role can use this endpoint
        /// </remarks>
        /// <response code="200">Report status changed successfully</response>
        /// <response code="400">If validation fails or required fields are missing</response>
        /// <response code="403">If the user is not an admin</response>
        /// <response code="404">If report, user, or admin not found</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpPatch("report/change-status")]
        public async Task<IActionResult> ChangeReportStatusAsync([FromBody] ChangeReportStatusRequest request)
        {
            var (adminEmail, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _reportService.ChangeReportStatusAsync(adminEmail, request);

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
                    errors = result.Errors,
                    data = result.Data
                });
        }
    }
}
