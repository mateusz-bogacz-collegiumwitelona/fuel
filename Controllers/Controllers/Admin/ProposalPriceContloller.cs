using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Services;

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
        /// Retrieves a paginated list of pending price proposals with optional search and sorting.
        /// </summary>
        /// <param name="pagged">Pagination parameters (page number and page size)</param>
        /// <param name="request">Table filtering and sorting parameters</param>
        /// <returns>A paginated list of price proposals</returns>
        /// <remarks>
        /// Sample request:
        ///
        ///     GET /api/priceproposal/list?PageNumber=1&amp;PageSize=10&amp;Search=benzyna&amp;SortBy=createdat&amp;SortDirection=desc
        ///
        /// Available sort fields:
        /// - username - Sort by user who created the proposal
        /// - brandname - Sort by gas station brand name
        /// - street - Sort by station street address
        /// - housenumber - Sort by station house number
        /// - city - Sort by station city
        /// - fuelname - Sort by fuel type name
        /// - fuelcode - Sort by fuel type code
        /// - proposedprice - Sort by proposed price value
        /// - createdat - Sort by creation date (default)
        ///
        /// Sort directions: asc (ascending) or desc (descending)
        ///
        /// Search will filter results across all displayed fields including username, brand name, address, fuel type, and price.
        /// </remarks>
        /// <response code="200">Returns the paginated list of price proposals</response>
        /// <response code="400">If the request parameters are invalid</response>
        /// <response code="500">If an internal server error occurs</response>
        [HttpGet("list")]
        public async Task<IActionResult> GetAllPriceProposal([FromQuery] GetPaggedRequest pagged, [FromQuery] TableRequest request)
        {
            var result = await _priceProposalServices.GetAllPriceProposal(pagged, request);

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
