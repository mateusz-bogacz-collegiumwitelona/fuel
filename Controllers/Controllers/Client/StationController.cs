using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Services.Helpers;
using Services.Interfaces;

namespace Contlollers.Controllers.Client
{
    [Route("api/station")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class StationController : ControllerBase
    {
        private readonly IStationServices _stationServices;

        public StationController(IStationServices stationServices)
        {
            _stationServices = stationServices;
        }

        /// <summary>
        /// Get all fuel stations for displaying pins and details on the map.
        /// </summary>
        /// <remarks>
        /// Description
        /// Returns a list of stations filtered by:
        /// - brand names (optional)
        /// - location and distance (optional)
        ///
        /// If no filters are provided, all stations are returned.
        ///
        /// Example request body
        /// Empty request – return all stations
        /// ```json
        /// {
        ///   "brandName": [],
        ///   "locationLatitude": null,
        ///   "locationLongitude": null,
        ///   "distance": null
        /// }
        /// ```
        ///
        /// Filter by brand and location within 10 km
        /// ```json
        /// {
        ///   "brandName": ["Orlen", "Shell"],
        ///   "locationLatitude": 52.4064,
        ///   "locationLongitude": 16.9252,
        ///   "distance": 10
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// [
        ///   {
        ///     "brandName": "Orlen",
        ///     "street": "Głogowska",
        ///     "houseNumber": "25",
        ///     "city": "Poznań",
        ///     "postalCode": "60-702",
        ///     "latitude": 52.394,
        ///     "longitude": 16.881
        ///   },
        ///   {
        ///     "brandName": "Shell",
        ///     "street": "Hetmańska",
        ///     "houseNumber": "10",
        ///     "city": "Poznań",
        ///     "postalCode": "60-251",
        ///     "latitude": 52.385,
        ///     "longitude": 16.915
        ///   }
        /// ]
        /// ```
        ///
        /// Notes
        /// - `distance` is in **kilometers**
        /// - `locationLatitude` / `locationLongitude` must be in **WGS84 format**
        ///
        /// </remarks>
        /// <response code="200">Everything is fine – stations successfully retrieved</response>
        /// <response code="404">No stations found or validation error</response>
        /// <response code="500">Something went wrong on the server (pray to the God Emperor)</response>

        [HttpPost("map/all")]
        public async Task<IActionResult> GetAllStationsForMapAsync(GetStationsRequest request)
        {
            var result = await _stationServices.GetAllStationsForMapAsync(request);
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
        /// Get nearest stations. Autommatly response only 3
        /// </summary>
        /// <param name="latitude">Client latitude</param>
        /// <param name="longitude">Client longitude</param>
        /// <param name="count">How many Stations they can see</param>
        /// <remarks>
        /// Endpoint return a list of stations with their
        /// [{
        /// "brandName": string,
        /// "address": string,
        /// "latitude": double,
        ///  "longitude": double
        /// },...]
        /// </remarks>
        /// <response code="200">Everything is fine</response>
        /// <response code="404">Can't find stations / Validation error</response>
        /// <response code="500">Something bad in backend. Pray to god emperor</response>

        [HttpGet("map/nearest")]
        public async Task<IActionResult> GetNearestStationAsync(
            [FromQuery] double latitude,
            [FromQuery] double longitude,
            [FromQuery] int? count
            )
        {
            var result = await _stationServices.GetNearestStationAsync(latitude, longitude, count);
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
        /// Retrieve a paginated list of fuel stations based on filters and sorting options.
        /// </summary>
        /// <remarks>
        /// Description
        /// Returns a list of fuel stations that match the specified criteria.
        /// You can filter by location (latitude, longitude, distance), fuel type, price range, and brand name.
        /// Results can be sorted by distance or price, and are returned with pagination support.
        /// 
        /// Example request body - Basic pagination
        /// ```json
        /// {
        ///   "pagging": {
        ///     "pageNumber": 1,
        ///     "pageSize": 10
        ///   }
        /// }
        /// ```
        ///
        /// Example request body - Filter by location and distance
        /// ```json
        /// {
        ///   "locationLatitude": 51.21006,
        ///   "locationLongitude": 16.1619,
        ///   "distance": 10,
        ///   "fuelType": ["PB95", "ON"],
        ///   "sortingByDisance": true,
        ///   "sortingDirection": "asc",
        ///   "pagging": {
        ///     "pageNumber": 1,
        ///     "pageSize": 20
        ///   }
        /// }
        /// ```
        ///
        /// Example request body - Filter by fuel type and price range
        /// ```json
        /// {
        ///   "fuelType": ["PB95"],
        ///   "minPrice": 5.50,
        ///   "maxPrice": 6.50,
        ///   "sortingByPrice": true,
        ///   "sortingDirection": "asc",
        ///   "pagging": {
        ///     "pageNumber": 1,
        ///     "pageSize": 10
        ///   }
        /// }
        /// ```
        ///
        /// Example request body - Filter by brand
        /// ```json
        /// {
        ///   "brandName": "Orlen",
        ///   "fuelType": ["PB95"],
        ///   "pagging": {
        ///     "pageNumber": 1,
        ///     "pageSize": 10
        ///   }
        /// }
        /// ```
        ///
        /// Example response
        /// ```json
        /// {
        ///   "items": [
        ///     {
        ///       "brandName": "Orlen",
        ///       "street": "Main Street",
        ///       "houseNumber": "123",
        ///       "city": "Warsaw",
        ///       "postalCode": "00-001",
        ///       "latitude": 52.2297,
        ///       "longitude": 21.0122,
        ///       "fuelPrice": [
        ///         {
        ///           "fuelCode": "PB95",
        ///           "price": 6.29,
        ///           "validFrom": "2025-10-22T10:00:00Z"
        ///         }
        ///       ]
        ///     }
        ///   ],
        ///   "pageNumber": 1,
        ///   "pageSize": 10,
        ///   "totalCount": 45,
        ///   "totalPages": 5,
        ///   "hasPreviousPage": false,
        ///   "hasNextPage": true
        /// }
        /// ```
        ///
        /// Filter Parameters
        /// - **locationLatitude** (optional): Latitude coordinate (-90 to 90)
        /// - **locationLongitude** (optional): Longitude coordinate (-180 to 180)
        /// - **distance** (optional): Search radius in kilometers (requires both latitude and longitude)
        /// - **fuelType** (optional): List of fuel type names (e.g., ["PB95", "ON", "LPG"])
        /// - **minPrice** (optional): Minimum fuel price (requires fuelType to be specified)
        /// - **maxPrice** (optional): Maximum fuel price (requires fuelType to be specified)
        /// - **brandName** (optional): Filter by station brand name (case-insensitive)
        ///
        /// Sorting Parameters
        /// - **sortingByDisance** (optional): Sort by distance from location (requires latitude and longitude)
        /// - **sortingByPrice** (optional): Sort by fuel price (requires fuelType to be specified)
        /// - **sortingDirection** (optional): Sort direction - "asc" (default) or "desc"
        /// - Note: Cannot sort by both distance and price simultaneously
        ///
        /// Pagination Parameters
        /// - **pageNumber** (optional): Page number to retrieve (default: 1, must be > 0)
        /// - **pageSize** (optional): Number of items per page (default: 10, range: 1-100)
        /// - If requested page exceeds total pages, the last available page is returned automatically
        ///
        /// Notes
        /// - All parameters are optional except pagination (which uses defaults if not provided)
        /// - Empty `fuelType` array returns all fuel types
        /// - Distance filtering uses kilometers and calculates straight-line distance
        /// - Price filtering requires at least one fuel type to be specified
        /// - Sorting by price requires at least one fuel type to be specified
        /// - Sorting by distance requires location coordinates to be provided
        /// - If no stations match the criteria, an empty result with pagination info is returned
        /// </remarks>
        /// <response code="200">Stations retrieved successfully (may be empty if no matches found)</response>
        /// <response code="400">
        /// Validation error - invalid parameters provided. Common errors:
        /// - Invalid page number (must be > 0)
        /// - Invalid page size (must be between 1 and 100)
        /// - Invalid latitude (must be between -90 and 90)
        /// - Invalid longitude (must be between -180 and 180)
        /// - Invalid fuel type name
        /// - Negative price values
        /// - Invalid brand name
        /// - Conflicting sorting options (cannot sort by both price and distance)
        /// - Sorting by distance without location coordinates
        /// - Sorting or filtering by price without specifying fuel type
        /// </response>
        /// <response code="500">Server error — something went wrong while processing the request</response>
        [HttpPost("list")]
        public async Task<IActionResult> GetStationListAsync(GetStationListRequest request)
        {
            var result = await _stationServices.GetStationListAsync(request);
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
