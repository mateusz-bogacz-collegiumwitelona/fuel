using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Helpers;
using Services.Interfaces;
using System.Security.Claims;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/user")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;

        public UserController(IUserServices userServices)
        {
            _userServices = userServices;
        }


        /// <summary>
        /// Retrieves a paginated and filterable list of users
        /// </summary>
        /// <remarks>
        /// Returns a complete list of users (non-deleted) with their assigned roles and creation dates.
        /// Supports searching, sorting, and pagination for admin management purposes.
        /// 
        /// **Features:**
        /// - Filter users by username, email, or role name
        /// - Sort by username, email, role, or creation date
        /// - Paginate results using `PageNumber` and `PageSize`
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
        ///             "createdAt": "2025-11-07T14:38:54.705875Z"
        ///           },
        ///           {
        ///             "userName": "User1",
        ///             "email": "user1@example.pl",
        ///             "roles": "User",
        ///             "createdAt": "2025-11-07T14:38:55.153691Z"
        ///           }
        ///         ],
        ///         "pageNumber": 1,
        ///         "pageSize": 10,
        ///         "totalCount": 2,
        ///         "totalPages": 1
        ///       }
        ///     }
        /// 
        /// **Use Case:**
        /// - Used in the admin panel to view and manage users
        /// - Can be combined with UI search/sort controls for live filtering
        /// 
        /// **Supported Sorting Fields:**
        /// - `username` → alphabetical order
        /// - `email` → alphabetical order
        /// - `roles` → by role priority (Admin > User)
        /// - `createdAt` → by account creation date
        /// 
        /// </remarks>
        /// <param name="pagged">Pagination configuration (page number and page size)</param>
        /// <param name="request">Search and sorting configuration (search text, sort field, sort direction)</param>
        /// <returns>Paginated list of users with role and account details</returns>
        /// <response code="200">Users retrieved successfully</response>
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
        ///     PUT /api/admin/user/change-role?email=user@example.pl&newRole=Admin
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

        [HttpPut("change-role")]
        public async Task<IActionResult> ChangeUserRoleAsync([FromQuery]string email, [FromQuery]string newRole)
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

        [HttpPost("lock-out")]
        public async Task<IActionResult> LockoutUserAsync(SetLockoutForUserRequest request)
        {
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;

            if (string.IsNullOrEmpty(adminEmail))
            {
                return Unauthorized(new
                {
                    success = false,
                    message = "User not authenticated"
                });

            }

            var result = await _userServices.LockoutUserAsync(adminEmail, request);
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
