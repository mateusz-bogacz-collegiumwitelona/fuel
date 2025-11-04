using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/proposal")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class ProposalPriceContloller : ControllerBase
    {
        private readonly IPriceProposalServices _priceProposalServices;

        public ProposalPriceContloller(IPriceProposalServices priceProposalServices)
        {
            _priceProposalServices = priceProposalServices;
        }

        /// <summary>
        /// Retrieve a price proposal by its photo token.
        /// </summary>
        /// <remarks>
        /// Description: Returns detailed information about a specific fuel price proposal, including station details, fuel type, proposed price, and a temporary presigned URL to the verification photo.
        ///
        /// Example request
        /// ```http
        /// GET /api/proposal/abc123def456
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "email": "user@example.pl",
        ///   "brandName": "Orlen",
        ///   "street": "Ignacego Domejki",
        ///   "houseNumber": "1a",
        ///   "city": "Legnica",
        ///   "postalCode": "59-220",
        ///   "fuelType": "ON",
        ///   "proposedPrice": 4.44,
        ///   "photoUrl": "http://localhost:9000/fuel-prices/...",
        ///   "createdAt": "2025-10-27T15:31:18.372993Z"
        /// }
        /// ```
        ///
        /// Notes
        /// - `photoUrl` is a temporary presigned URL valid for 1 hour (3600 seconds) from generation.
        /// - `photoToken` is a unique identifier assigned to each price proposal for secure access.
        /// - `proposedPrice` is the fuel price in PLN per liter submitted by the user.
        /// - `createdAt` is the timestamp (UTC) when the proposal was submitted.
        /// - The photo token does not expose internal database IDs for security purposes.
        /// </remarks>
        /// <param name="token">Unique photo token identifier for the price proposal.</param>
        /// <response code="200">Price proposal successfully retrieved.</response>
        /// <response code="400">Validation error - photo token is null or empty.</response>
        /// <response code="404">Price proposal not found with the provided photo token.</response>
        /// <response code="500">Unexpected server error occurred while processing the request.</response>
        [HttpGet("{token}")]
        public async Task<IActionResult> GetPriceProposal(string token)
        {
            var result = await _priceProposalServices.GetPriceProposal(token);

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
    }
}
