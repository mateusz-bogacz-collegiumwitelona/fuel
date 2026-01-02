using Controllers.Controllers;
using DTO.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Contlollers.Controllers.Client
{
    [Route("api/proposal-statistic")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class ProposalStatisticController : AuthControllerBase
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
        /// GET /api/proposals-statistics
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
        [HttpGet()]
        public async Task<IActionResult> GetUserProposalStatisticResponse()
        {
            var (email, error) = GetAuthenticatedUser();
            if (error != null) return error;

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

        /// <summary>
        /// Retrieve a paginated list of top users ranked by their proposal points.
        /// </summary>
        /// <remarks>
        /// Returns users ranked by their total points (earned from approved proposals), with support for pagination.
        /// Users are sorted in descending order by points. If no pagination parameters are provided, defaults to page 1 with 10 items per page.
        ///
        /// Example request
        /// ```http
        /// GET /api/proposals/top-users?PageNumber=1&amp;PageSize=10
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "items": [
        ///     {
        ///       "userName": "User1",
        ///       "totalProposals": 20,
        ///       "approvedProposals": 12,
        ///       "rejectedProposals": 8,
        ///       "acceptedRate": 60,
        ///       "points": 12
        ///     },
        ///     {
        ///       "userName": "User6",
        ///       "totalProposals": 18,
        ///       "approvedProposals": 11,
        ///       "rejectedProposals": 7,
        ///       "acceptedRate": 61,
        ///       "points": 11
        ///     },
        ///     {
        ///       "userName": "User8",
        ///       "totalProposals": 12,
        ///       "approvedProposals": 11,
        ///       "rejectedProposals": 1,
        ///       "acceptedRate": 91,
        ///       "points": 11
        ///     }
        ///   ],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 3,
        ///   "totalPages": 1
        /// }
        /// ```
        ///
        /// Notes:
        /// - Users are ranked by total points earned from approved proposals
        /// - `acceptedRate` represents the percentage of approved proposals out of total proposals
        /// - Default pagination: PageNumber = 1, PageSize = 10
        /// - If requested page number exceeds total pages, the last available page is returned
        /// - Empty list is returned if no users have proposal statistics
        /// </remarks>
        /// <param name="request">Pagination parameters (PageNumber and PageSize)</param>
        /// <response code="200">Top users list successfully retrieved (may be empty)</response>
        /// <response code="400">Invalid pagination parameters</response>
        /// <response code="500">Unexpected server error</response>
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
