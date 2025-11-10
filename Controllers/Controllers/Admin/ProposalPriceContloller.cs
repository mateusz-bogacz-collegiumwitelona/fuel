using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using Services.Services;
using System.Security.Claims;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/proposal")]
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
        ///
        /// Sample response:
        ///
        ///     {
        ///       "items": [
        ///         {
        ///           "userName": "User3",
        ///           "brandName": "Moya",
        ///           "street": "Poznańska",
        ///           "houseNumber": "112",
        ///           "city": "Łowicz",
        ///           "fuelName": "E85",
        ///           "fuelCode": "E85",
        ///           "proposedPrice": 4.74,
        ///           "status": "Pending",
        ///           "token": "930f26dd200141bcac45416fe745696f",
        ///           "createdAt": "2025-11-09T16:57:26.402333Z"
        ///         },
        ///         {
        ///           "userName": "User6",
        ///           "brandName": "Circle K",
        ///           "street": "Adamówek",
        ///           "houseNumber": "16",
        ///           "city": "Ozorków",
        ///           "fuelName": "LPG",
        ///           "fuelCode": "LPG",
        ///           "proposedPrice": 6.78,
        ///           "status": "Pending",
        ///           "token": "ff5355b9e6a44011851bf58252aff58f",
        ///           "createdAt": "2025-11-09T16:57:26.420039Z"
        ///         }
        ///       ],
        ///       "pageNumber": 1,
        ///       "pageSize": 10,
        ///       "totalCount": 25,
        ///       "totalPages": 3,
        ///       "hasPreviousPage": false,
        ///       "hasNextPage": true
        ///     }
        ///
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
        /// - `token` is a unique identifier assigned to each price proposal for secure access.
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
        public async Task<IActionResult> GetPriceProposal([FromRoute]string token)
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

        /// <summary>
        /// Accept or reject a pending price proposal submitted by a user.
        /// </summary>
        /// <remarks>
        /// Allows an administrator to review and change the status of a price proposal.
        /// 
        /// **When a proposal is accepted:**
        /// - The fuel price at the station is created or updated with the proposed price
        /// - The proposal status changes to Accepted
        /// - The user's proposal statistics are updated (approved count and points increase)
        /// 
        /// **When a proposal is rejected:**
        /// - The proposal status changes to Rejected
        /// - The user's proposal statistics are updated (rejected count increases)
        /// - No changes are made to station fuel prices
        /// 
        /// **Important Notes:**
        /// - Only proposals with Pending status can be reviewed
        /// - Each proposal can only be reviewed once (idempotent operation)
        /// - Admin email is automatically extracted from JWT token
        /// - The operation uses database transactions to ensure data consistency
        /// 
        /// Example request (Accept):
        /// ```
        /// POST /api/proposals/change-status/abc123token?isAccepted=true
        /// ```
        /// 
        /// Example request (Reject):
        /// ```
        /// POST /api/proposals/change-status/abc123token?isAccepted=false
        /// ```
        /// 
        /// Example success response (200 OK):
        /// ```json
        /// {
        ///   "message": "Price proposal accepted successfully",
        ///   "data": true
        /// }
        /// ```
        /// 
        /// Example error response - already reviewed (409 Conflict):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "This proposal has already been reviewed",
        ///   "errors": []
        /// }
        /// ```
        /// 
        /// Example error response - not found (404 Not Found):
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Price proposal not found",
        ///   "errors": []
        /// }
        /// ```
        /// </remarks>
        /// <param name="token">Unique token identifying the price proposal to review</param>
        /// <param name="isAccepted">True to accept the proposal, false to reject it</param>
        /// <returns>Operation result indicating success or failure</returns>
        /// <response code="200">Proposal status changed successfully</response>
        /// <response code="400">Invalid token or missing parameters</response>
        /// <response code="401">Unauthorized - valid JWT token with Admin role required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="404">Price proposal not found or not in Pending status</response>
        /// <response code="409">Conflict - proposal has already been reviewed</response>
        /// <response code="500">Internal server error or database transaction failed</response>
        [HttpPatch("change-status/{token}")]
        public async Task<IActionResult> ChangePriceProposalStatus([FromRoute]string token, [FromQuery]bool isAccepted)
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

            var result = await _priceProposalServices.ChangePriceProposalStatus(adminEmail, isAccepted, token);

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
