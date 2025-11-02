using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
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
    }
}
