using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Contlollers.Controllers.Client
{
    [Route("api/proposal-statistic")]
    [ApiController]
    [EnableCors("AllowClient")]
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
        /// Description: Returns summarized statistics about all proposals submitted by the user with the given email.
        ///
        /// Example request
        /// ```http
        /// GET /api/proposals/statistics?email=user@example.pl
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
        /// Notes
        /// - `acceptedRate` is calculated as the percentage of approved proposals out of the total.
        /// - `updatedAt` is the timestamp (UTC) of the last statistics update.
        /// - The request requires a valid existing user email.
        /// </remarks>
        /// <param name="email">User's email address used to look up their proposal statistics.</param>
        /// <response code="200">Statistics successfully retrieved</response>
        /// <response code="400">Validation error or internal repository issue</response>
        /// <response code="404">User or statistics not found</response>
        /// <response code="500">Unexpected server error</response>
        [HttpGet]
        public async Task<IActionResult> GetUserProposalStatisticResponse(string email)
        {
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

    }
}
