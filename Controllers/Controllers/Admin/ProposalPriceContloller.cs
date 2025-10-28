using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/proposal")]
    [EnableCors("AllowClient")]
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
        ///   "photoUrl": "http://localhost:9000/fuel-prices/fuel-prices/2025/10/27/bf06dcd0-5de2-423f-9668-1b41e9cf37c0.png?X-Amz-Algorithm=AWS4-HMAC-SHA256&X-Amz-Credential=user%2F20251028%2Fus-east-1%2Fs3%2Faws4_request&X-Amz-Date=20251028T214835Z&X-Amz-Expires=3600&X-Amz-SignedHeaders=host&X-Amz-Signature=30abd7f03c798ea2d1b5d880c52a2c01b2d0544394d830b411861d514c5cc005",
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
