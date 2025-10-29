using DTO.Requests;
using Microsoft.AspNetCore.Cors;
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
        private readonly IPriceProposalServices _priceProposalServices;
        public StationController(
            IStationServices stationServices,
            IPriceProposalServices priceProposalServices)
        {
            _stationServices = stationServices;
            _priceProposalServices = priceProposalServices;
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
        /// Returns a list of fuel stations that match the specified criteria.
        /// You can filter by location (latitude, longitude, distance), fuel type, price range, and brand name.
        /// Results can be sorted by distance or price, and are returned with pagination support.
        /// 
        /// Example request body - No filters, no sorting, automatic pagination
        /// ```json
        /// {
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": null,
        ///    "pageSize": null
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by distance with page navigation
        /// ```json
        /// {
        ///  "locationLatitude": 51.21006,
        ///  "locationLongitude": 16.1619,
        ///  "distance": 200,
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": 2,
        ///    "pageSize": 10
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by fuel type only
        /// ```json
        ///{
        ///  "fuelType": ["LPG"],
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": 1,
        ///    "pageSize": null
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by fuel type and price range
        /// ```json
        ///{
        ///  "fuelType": ["LPG"],
        ///  "minPrice": 3.50,
        ///  "maxPrice": 5.00,
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": 1,
        ///    "pageSize": null
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by brand name only
        /// ```json
        ///{
        ///  "brandName": "Orlen",
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": 2,
        ///    "pageSize": 7
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by location and fuel type
        /// ```json
        ///{
        ///  "locationLatitude": 51.21006,
        ///  "locationLongitude": 16.1619,
        ///  "distance": 10,
        ///  "fuelType": ["LPG"],
        ///  "sortingByDisance": null,
        ///  "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///  "pagging": {
        ///    "pageNumber": null,
        ///    "pageSize": null
        ///  }
        ///}
        /// ```
        ///
        /// Example request body - Filter by location, fuel type, and minimum price
        /// ```json
        /// {
        ///   "locationLatitude": 51.21006,
        ///   "locationLongitude": 16.1619,
        ///   "distance": 100,
        ///   "fuelType": ["LPG"],
        ///   "minPrice": 5,
        ///   "sortingByDisance": null,
        ///   "sortingByPrice": null,
        ///   "sortingDirection": null,
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
        ///
        /// Example request body - Filter by location, fuel types, maximum price, and brand
        /// ```json
        /// {
        ///   "locationLatitude": 51.21006,
        ///   "locationLongitude": 16.1619,
        ///   "distance": 100,
        ///   "fuelType": ["LPG", "E85"],
        ///   "minPrice": null,
        ///   "maxPrice": 5,
        ///   "brandName": "Orlen",
        ///   "sortingByDisance": null,
        ///   "sortingByPrice": null,
        ///   "sortingDirection": null,
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
        ///
        /// Example request body - Full filters with sorting by distance (descending)
        /// ```json
        /// {
        ///   "locationLatitude": 51.21006,
        ///   "locationLongitude": 16.1619,
        ///   "distance": 100,
        ///   "fuelType": ["LPG", "E85"],
        ///   "maxPrice": 5,
        ///   "brandName": "Orlen",
        ///   "sortingByDisance": true,
        ///   "sortingByPrice": null,
        ///   "sortingDirection": "desc",
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
        /// Example request body - Full filters with sorting by distance (asceding) (basic sort direction)
        /// ```json
        /// {
        ///   "locationLatitude": 51.21006,
        ///   "locationLongitude": 16.1619,
        ///   "distance": 100,
        ///   "fuelType": ["LPG", "E85"],
        ///   "maxPrice": 5,
        ///   "brandName": "Orlen",
        ///   "sortingByDisance": true,
        ///   "sortingByPrice": null,
        ///   "sortingDirection": "asc",
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
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


        /// <summary>
        /// Get all fuel station brands.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns a list of all available fuel station brands in the database.  
        /// Example response  
        /// ```json
        /// [
        ///   "Orlen",
        ///   "Shell",
        ///   "BP",
        ///   "Circle K",
        ///   "Lotos",
        ///   ...
        /// ]
        /// ```
        ///
        /// Notes  
        /// - The response contains **unique brand names** only.  
        /// - If no brands are found in the database, a `404` response is returned.  
        ///
        /// </remarks>
        /// <response code="200">Brands successfully retrieved</response>
        /// <response code="404">No brands found</response>
        /// <response code="500">An unexpected server error occurred</response>
        [HttpGet("all-brands")]
        public async Task<IActionResult> GetAllBrandsAsync()
        {
            var result = await _stationServices.GetAllBrandsAsync(); 
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
        /// Get detailed profile of a specific fuel station.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns complete information about a specific fuel station, including address, coordinates, brand name, and current fuel prices.  
        /// The station is identified based on the provided address (street, house number, city, postal code).  
        ///
        /// Example request body  
        /// ```json
        /// {
        ///   "street": "Ignacego Domejki",
        ///   "houseNumber": "1a",
        ///   "city": "Legnica",
        ///   "postalCode": "59-220"
        /// }
        /// ```
        ///
        /// Example response  
        /// ```json
        /// {
        ///   "brandName": "Orlen",
        ///   "street": "Ignacego Domejki",
        ///   "houseNumber": "1a",
        ///   "city": "Legnica",
        ///   "postalCode": "59-220",
        ///   "latitude": 51.2094953,
        ///   "longitude": 16.1309152,
        ///   "fuelPrice": [
        ///     {
        ///       "fuelCode": "PB95",
        ///       "price": 4.56,
        ///       "validFrom": "0001-01-01T00:00:00"
        ///     },
        ///     {
        ///       "fuelCode": "PB98",
        ///       "price": 6.8,
        ///       "validFrom": "0001-01-01T00:00:00"
        ///     },
        ///     {
        ///       "fuelCode": "LPG",
        ///       "price": 6.08,
        ///       "validFrom": "0001-01-01T00:00:00"
        ///     },
        ///     {
        ///       "fuelCode": "ON",
        ///       "price": 4.71,
        ///       "validFrom": "0001-01-01T00:00:00"
        ///     },
        ///     {
        ///       "fuelCode": "E85",
        ///       "price": 5.52,
        ///       "validFrom": "0001-01-01T00:00:00"
        ///     }
        ///   ]
        /// }
        /// ```
        ///
        /// Notes  
        /// - All request parameters are **required** for the search (street, houseNumber, city, postalCode).  
        /// - If no station matches the provided data, a `404` response is returned.  
        /// - The coordinate system used is **WGS84** (latitude, longitude).  
        /// - The `fuelPrice` list may contain multiple entries depending on the available fuel types.  
        ///
        /// </remarks>
        /// <response code="200">Station profile successfully retrieved</response>
        /// <response code="404">No matching station found</response>
        /// <response code="500">Server error — something went wrong while processing the request</response>
        [HttpPost("profile")]
        public async Task<IActionResult> GetStationProfileAsync(GetStationProfileRequest request)
        {
            var result = await _stationServices.GetStationProfileAsync(request);
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
        /// Submit a new fuel price proposal with verification photo.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Allows authenticated users to submit a fuel price proposal for a specific gas station, including a verification photo.  
        /// The photo is uploaded to MinIO storage and a unique proposal record is created in the database.  
        /// The station is identified based on the provided data (brand name, street, house number, city).  
        ///
        /// Example request (multipart/form-data)  
        /// ```
        /// POST /api/price-proposal/add
        /// Content-Type: multipart/form-data
        ///
        /// Email: user@example.pl
        /// BrandName: Orlen
        /// Street: Ignacego Domejki
        /// HouseNumber: 1a
        /// City: Legnica
        /// FuelType: ON
        /// ProposedPrice: 6.89
        /// Photo: [binary file: image.jpg]
        /// ```
        ///
        /// Example success response  
        /// ```json
        /// {
        ///   "message": "Price proposal added successfully in 1234"
        /// }
        /// ```
        ///
        /// Example error response  
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Validation error",
        ///   "errors": [
        ///     "Invalid photo file type. Allowed types are: JPEG, JPG, PNG, WEBP."
        ///   ]
        /// }
        /// ```
        ///
        /// Validation rules  
        /// - **Email**: Required, must be a valid email format, user must exist in the system.
        /// - **BrandName**: Gas station brand name (e.g., Orlen, Shell, BP). Required.
        /// - **Street**: Street name where the station is located. Required.
        /// - **HouseNumber**: Street number of the station. Required.
        /// - **City**: City where the station is located. Required.
        /// - **FuelType**: Must be one of: `PB95`, `PB98`, `ON`, `LPG`, `E85`. Required.
        /// - **ProposedPrice**: Must be greater than 0, represents price in PLN per liter. Required.
        /// - **Photo**: Required, max size **5 MB**, allowed formats: **JPEG, JPG, PNG, WEBP**.
        ///
        /// Notes  
        /// - The station must exist in the database (matched by brand name, street, house number, and city).
        /// - The user must be registered and authenticated in the system.
        /// - The photo is stored in MinIO with a unique filename based on the proposal ID.
        /// - All operations are executed within a database transaction with automatic rollback on failure.
        /// - If the database save fails, the uploaded photo is automatically cleaned up from MinIO.
        /// - The proposal is initially created with status **Pending** and requires admin approval.
        ///
        /// </remarks>
        /// <param name="request">Multipart form data containing proposal details and verification photo</param>
        /// <response code="200">Price proposal successfully added</response>
        /// <response code="400">Validation error — invalid data format, file type, size, missing station or user</response>
        /// <response code="500">Server error — something went wrong while processing the request</response>
        [HttpPost("price-proposal/add")]
        public async Task<IActionResult> AddNewPriceProposalAsync([FromForm] AddNewPriceProposalRequest request)
        {
            var result = await _priceProposalServices.AddNewProposalAsync(request);
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
