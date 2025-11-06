using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/station")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class StationController : ControllerBase
    {
        private readonly IStationServices _stationServices;

        public StationController(IStationServices stationServices)
        {
            _stationServices = stationServices;
        }

        /// <summary>
        /// Retrieve a paginated, searchable, and sortable list of fuel stations for administrators.
        /// </summary>
        /// <remarks>
        /// Returns a list of fuel stations with related address details (street, house number, city, postal code)  
        /// and brand information. Supports **search**, **sorting**, and **pagination** to help administrators  
        /// efficiently manage stations in the admin panel.
        ///
        /// Example request  
        /// ```http
        /// GET /api/admin/station/list?Search=Warszawa&amp;PageNumber=1&amp;PageSize=10&amp;SortBy=brandname&amp;SortDirection=asc
        /// ```
        ///
        /// Example response — Successful retrieval  
        /// ```json
        /// {
        ///   "items": [
        ///     {
        ///       "brandName": "Orlen",
        ///       "street": "Puławska",
        ///       "houseNumber": "12A",
        ///       "city": "Warszawa",
        ///       "postalCode": "02-512",
        ///       "createdAt": "2025-11-06T12:14:23.569787Z",
        ///       "updatedAt": "2025-11-06T12:14:23.569787Z"
        ///     },
        ///     {
        ///       "brandName": "BP",
        ///       "street": "Beskidzka",
        ///       "houseNumber": "15",
        ///       "city": "Kraków",
        ///       "postalCode": "30-611",
        ///       "createdAt": "2025-11-06T12:15:11.219Z",
        ///       "updatedAt": "2025-11-06T12:15:11.219Z"
        ///     }
        ///   ],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 2,
        ///   "totalPages": 1
        /// }
        /// ```
        ///
        /// Example response — No stations found  
        /// ```json
        /// {
        ///   "items": [],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 0,
        ///   "totalPages": 0
        /// }
        /// ```
        ///
        /// Example response — Server error  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "An error occurred while processing your request.",
        ///   "errors": [ "Object reference not set to an instance of an object." ],
        ///   "data": null
        /// }
        /// ```
        ///
        /// Notes  
        /// - `Search` — optional query parameter; filters by brand name, street, city, or postal code.  
        /// - `SortBy` — optional query parameter; accepts one of: `brandname`, `street`, `housenumber`, `city`, `postalcode`, `createdat`, `updatedat`.  
        /// - `SortDirection` — optional query parameter; accepts `asc` or `desc` (default: ascending).  
        /// - `PageNumber` and `PageSize` — control pagination (default: 1 and 10 respectively).  
        /// </remarks>
        /// <response code="200">Stations retrieved successfully (even if list is empty)</response>
        /// <response code="400">Invalid pagination or query parameters</response>
        /// <response code="500">Server error while retrieving station list</response>

        [HttpGet("list")]
        public async Task<IActionResult> GetStationsListForAdminAsync(
            [FromQuery] GetPaggedRequest pagged,
            [FromQuery] TableRequest request)
        {
            var result = await _stationServices.GetStationsListForAdminAsync(pagged, request);
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
