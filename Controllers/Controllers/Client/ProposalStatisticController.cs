using DTO.Requests;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace Contlollers.Controllers.Client
{
    [Route("api/proposal-statistic")]
    [ApiController]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "User,Admin")]
    public class ProposalStatisticController : ControllerBase
    {
        private readonly IProposalStatisticServices _proposalStatistic;

        public ProposalStatisticController(IProposalStatisticServices proposalStatistic)
        {
            _proposalStatistic = proposalStatistic;
        }

        /// <summary>
        /// Retrieve proposal statistics for a user by their email address.
        /// </summary>
        /// <remarks>
        /// Returns summarized statistics about all proposals submitted by the user with the given email.
        /// The user's email is automatically extracted from the JWT token, ensuring users can only access their own statistics.
        ///
        /// Example request
        /// ```http
        /// GET /api/proposals/statistics
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "totalProposals": 42,
        ///   "approvedProposals": 30,
        ///   "rejectedProposals": 12,
        ///   "acceptedRate": 71,
        ///   "updatedAt": "2025-10-17T12:34:56Z"
        /// }
        /// ```
        ///
        /// Notes:
        /// - `acceptedRate` is calculated as the percentage of approved proposals out of the total.
        /// - `updatedAt` is the timestamp (UTC) of the last statistics update.
        /// - User email is automatically retrieved from JWT token claims.
        /// - No email parameter is required - the endpoint always returns data for the authenticated user.
        /// </remarks>
        /// <param name="email">User's email address used to look up their proposal statistics.</param>
        /// <response code="200">Statistics successfully retrieved</response>
        /// <response code="400">Internal repository issue</response>
        /// <response code="401">Unauthorize</response>
        /// <response code="404">User or statistics not found</response>
        /// <response code="500">Unexpected server error</response>
        [HttpGet]
        public async Task<IActionResult> GetUserProposalStatisticResponse()
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

            var result = await _proposalStatistic.GetUserProposalStatisticResponse(email);
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors
                });
        }

        [HttpGet("top-users")]
        public async Task<IActionResult> GetTopUserListAsync([FromQuery] GetPaggedRequest request)
        {
            var result = await _proposalStatistic.GetTopUserListAsync(request);
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
