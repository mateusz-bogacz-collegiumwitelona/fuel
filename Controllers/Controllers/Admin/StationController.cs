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
        /// Edits an existing fuel station's details
        /// </summary>
        /// <remarks>
        /// Updates station information including brand, address, location coordinates, and fuel prices.
        /// All fields except FindStation are optional - only provided fields will be updated.
        /// 
        /// Sample request:
        /// 
        ///     PUT /api/admin/station/edit
        ///     {
        ///       "findStation": {
        ///         "brandName": "Orlen",
        ///         "street": "Główna",
        ///         "houseNumber": "1a",
        ///         "city": "Warszawa"
        ///       },
        ///       "newBrandName": "Shell",
        ///       "newStreet": "Nowa",
        ///       "newHouseNumber": "5b",
        ///       "newCity": "Kraków",
        ///       "newLatitude": 50.0647,
        ///       "newLongitude": 19.9450,
        ///       "fuelType": [
        ///         {
        ///           "code": "PB95",
        ///           "price": 6.50
        ///         },
        ///         {
        ///           "code": "ON",
        ///           "price": 6.80
        ///         }
        ///       ]
        ///     }
        ///     
        /// **Partial Updates:**
        /// - You can update only specific fields by omitting others
        /// - Example: Send only "newStreet" to update street while keeping other data unchanged
        /// - Location coordinates require both latitude AND longitude
        /// - Fuel types: provided list will replace existing fuels (add new, update prices, remove unlisted)
        /// 
        /// </remarks>
        /// <param name="request">Station edit request containing search criteria and new values</param>
        /// <returns>Result indicating success or failure of the operation</returns>
        /// <response code="200">Station updated successfully</response>
        /// <response code="400">Validation error (invalid brand name or station not found)</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>

        [HttpPatch("edit")]
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

        /// <summary>
        /// Retrieves station information for editing purposes
        /// </summary>
        /// <remarks>
        /// Gets complete station details including brand, address, location coordinates, and current fuel prices.
        /// This endpoint is typically used to populate an edit form with existing station data.
        /// Station is identified by brand name, street, house number, and city.
        /// 
        /// Sample request:
        /// 
        ///     GET /api/admin/station/edit/info?BrandName=Orlen&amp;Street=Główna&amp;HouseNumber=15A&amp;City=Warszawa
        ///     
        /// Sample response:
        /// 
        ///     {
        ///       "success": true,
        ///       "message": "Station info retrieved successfully.",
        ///       "data": {
        ///         "BrandName": "Orlen",
        ///         "Street": "Główna",
        ///         "HouseNumber": "15A",
        ///         "City": "Warszawa",
        ///         "Latitude": 52.2297,
        ///         "Longitude": 21.0122,
        ///         "fuelType": [
        ///           {
        ///             "code": "PB95",
        ///             "price": 6.50
        ///           },
        ///           {
        ///             "code": "PB98",
        ///             "price": 7.20
        ///           },
        ///           {
        ///             "code": "ON",
        ///             "price": 6.80
        ///           },
        ///           {
        ///             "code": "LPG",
        ///             "price": 3.20
        ///           }
        ///         ]
        ///       }
        ///     }
        ///     
        /// **Use Case:**
        /// - Call this endpoint before showing edit form to get current station data
        /// - Use the returned data to pre-fill form fields
        /// - User can then modify any fields and submit via PUT /api/admin/station/edit/{stationId}
        /// 
        /// **Response Fields:**
        /// - All fields represent current station data
        /// - Field names match the edit request format for easy form binding
        /// - FuelType array contains all currently available fuels at the station
        /// 
        /// </remarks>
        /// <param name="request">Station identification criteria (brand name, street, house number, city)</param>
        /// <returns>Complete station information ready for editing</returns>
        /// <response code="200">Station information retrieved successfully</response>
        /// <response code="404">Station not found with the provided criteria</response>
        /// <response code="400">Invalid request parameters</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpGet("edit/info")]
        public async Task<IActionResult> GetStationInfoForEdit([FromQuery] FindStationRequest request)
        {
            var result = await _stationServices.GetStationInfoForEdit(request);
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

        /// <summary>
        /// Adds a new fuel station to the system
        /// </summary>
        /// <remarks>
        /// Creates a new fuel station with specified brand, address, location coordinates, and fuel prices.
        /// All fields are required. You can add multiple fuel types by including them in the fuelTypes array.
        /// 
        /// Sample request:
        /// 
        ///     POST /api/admin/station/add
        ///     {
        ///       "brandName": "Orlen",
        ///       "street": "Główna",
        ///       "houseNumber": "15A",
        ///       "city": "Warszawa",
        ///       "postalCode": "00-001"
        ///       "latitude": 52.2297,
        ///       "longitude": 21.0122,
        ///       "fuelTypes": [
        ///         {
        ///           "code": "PB95",
        ///           "price": 6.50
        ///         },
        ///         {
        ///           "code": "PB98",
        ///           "price": 7.20
        ///         },
        ///         {
        ///           "code": "ON",
        ///           "price": 6.80
        ///         },
        ///         {
        ///           "code": "LPG",
        ///           "price": 3.20
        ///         }
        ///       ]
        ///     }
        ///     
        /// **Important Notes:**
        /// - Brand name must exist in the system (e.g., "Orlen", "Shell", "BP", "Circle K")
        /// - At least one fuel type is required
        /// - Fuel type codes must be valid (e.g., "PB95", "PB98", "ON", "LPG")
        /// - You can add as many fuel types as needed 
        /// - Coordinates must be valid GPS coordinates (latitude: -90 to 90, longitude: -180 to 180)
        /// - All fuel prices must be greater than 0.01
        /// 
        /// </remarks>
        /// <param name="request">Station details including brand, address, location, and fuel prices</param>
        /// <returns>Result indicating success or failure of the operation</returns>
        /// <response code="201">Station created successfully</response>
        /// <response code="400">Validation error (invalid brand name, fuel types, or missing required fields)</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpPost("add")]
        public async Task<IActionResult> AddNewStationAsync([FromBody] AddStationRequest request)
        {
            var result = await _stationServices.AddNewStationAsync(request);
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

        /// <summary>
        /// Deletes an existing fuel station from the system
        /// </summary>
        /// <remarks>
        /// Removes a fuel station identified by brand name and address details.
        /// This operation will cascade delete all associated data including:
        /// - Station address
        /// - All fuel prices for this station
        /// - All price proposals for this station
        /// 
        /// Sample request:
        /// 
        ///     DELETE /api/admin/station/delete
        ///     {
        ///       "brandName": "A-Prim",
        ///       "street": "Beskidzka",
        ///       "houseNumber": "15",
        ///       "city": "Grojec"
        ///     }
        ///     
        /// **Important Notes:**
        /// - All fields are required to uniquely identify the station
        /// - Brand name and address must match exactly (case-insensitive)
        /// - This operation is irreversible - all related data will be permanently deleted
        /// - Deleting a station will automatically remove:
        ///   * The station's address
        ///   * All fuel prices associated with this station
        ///   * All price proposals submitted for this station
        /// 
        /// </remarks>
        /// <param name="request">Station identification details (brand name and full address)</param>
        /// <returns>Result indicating success or failure of the deletion</returns>
        /// <response code="200">Station deleted successfully</response>
        /// <response code="400">Station not found with provided details</response>
        /// <response code="401">Unauthorized - valid JWT token required</response>
        /// <response code="403">Forbidden - Admin role required</response>
        /// <response code="500">Internal server error</response>
        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteStationAsync([FromBody] FindStationRequest request)
        {
            var result = await _stationServices.DeleteStationAsync(request);
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
