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

        /// <summary>
        /// Edit an existing fuel station's details.
        /// </summary>
        /// <remarks>
        /// **Description:**  
        /// Allows an administrator to update the details of an existing fuel station, such as brand, address, or geographic location.  
        /// The station to be edited is identified using the `FindStation` object, which includes the current brand name, street, house number, and city.
        ///
        /// **Example request**  
        /// ```http
        /// PUT /api/admin/station/edit
        /// Content-Type: application/json
        ///
        /// {
        ///   "findStation": {
        ///     "brandName": "Amic",
        ///     "street": "Strzelców Bytomskich",
        ///     "houseNumber": "66F",
        ///     "city": "Bytom"
        ///   },
        ///   "newBrandName": "Orlen",
        ///   "newStreet": "Katowicka",
        ///   "newHouseNumber": "120",
        ///   "newCity": "Katowice",
        ///   "newLatitude": 50.259,
        ///   "newLongitude": 19.022
        /// }
        /// ```
        ///
        /// **Example response — Successful update**  
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Station details updated successfully.",
        ///   "data": true
        /// }
        /// ```
        ///
        /// **Example response — Station not found**  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Failed to edit station details.",
        ///   "errors": [ "Could not update the station with the provided details." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// **Example response — Validation error (invalid coordinates)**  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": [ "Latitude must be between -90 and 90." ],
        ///   "data": false
        /// }
        /// ```
        ///
        /// **Notes**  
        /// - `findStation` — required; identifies the existing station to be updated.  
        /// - `newBrandName` — optional; if the specified brand does not exist, it will be created automatically.  
        /// - **Address update rule:**  
        ///   To update the address and geographic location, all of the following fields **must be provided** simultaneously:  
        ///   `newStreet`, `newHouseNumber`, `newCity`, `newLatitude`, and `newLongitude`.  
        ///   When these are provided, the system recalculates the spatial `Point` (`longitude`, `latitude`) with SRID = 4326 and updates the `UpdatedAt` timestamp of the address.  
        /// - Partial address updates (e.g., only changing the city) are ignored to maintain data integrity.  
        /// - Returns `true` if the update was successful.  
        /// </remarks>
        /// <response code="200">Station successfully updated</response>
        /// <response code="400">Validation error (invalid or missing data)</response>
        /// <response code="404">Station not found</response>
        /// <response code="500">Server error during update</response>

        [HttpPut("edit")]
        public async Task<IActionResult> EditStationAsync([FromBody] EditStationRequest request)
        {
            var result = await _stationServices.EditStationAsync(request);
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
