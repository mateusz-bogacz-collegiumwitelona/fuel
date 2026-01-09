using Azure.Core;
using Controllers.Controllers;
using DTO.Requests;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Services.Interfaces;
using System.Security.Claims;

namespace Contlollers.Controllers.Client
{
    [Route("api/station")]
    [ApiController]
    [EnableCors("AllowClient")]
    public class StationController : AuthControllerBase
    {
        private readonly IStationServices _stationServices;
        private readonly IPriceProposalServices _priceProposalServices;
        private readonly IFuelTypeServices _fuelTypeServices;
        private readonly IBrandServices _brandServices;
        public StationController(
            IStationServices stationServices,
            IPriceProposalServices priceProposalServices,
            IFuelTypeServices fuelTypeServices,
            IBrandServices brandServices)
        {
            _stationServices = stationServices;
            _priceProposalServices = priceProposalServices;
            _fuelTypeServices = fuelTypeServices;
            _brandServices = brandServices;
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
        /// Example request body - Filter by date after update with page navigation
        /// ```json
        /// {
        ///  "priceUpdatedAfter": "2025-11-10T00:00:00Z",
        /// "sortingByDisance": null,
        /// "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
        /// Example request body - Filter by date before update with page navigation
        /// ```json
        /// {
        ///  "priceUpdatedBefore": "2025-11-10T00:00:00Z",
        /// "sortingByDisance": null,
        /// "sortingByPrice": null,
        ///  "sortingDirection": null,
        ///   "pagging": {
        ///     "pageNumber": null,
        ///     "pageSize": null
        ///   }
        /// }
        /// ```
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
            var result = await _brandServices.GetAllBrandsAsync();
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
        /// The station is identified based on the provided information (brand, street, house number, city).  
        ///
        /// Example request  
        /// <code>
        /// GET api/station/profile?BrandName=Orlen&amp;Street=Ignacego%20Domejki&amp;HouseNumber=1a&amp;City=Legnica
        /// </code>
        ///
        /// Example response  
        /// <code>
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
        /// </code>
        ///
        /// Notes  
        /// - All request parameters are required for the search (brandName, street, houseNumber, city).  
        /// - If no station matches the provided data, a 404 response is returned.  
        /// - The coordinate system used is WGS84 (latitude, longitude).  
        /// - The fuelPrice list may contain multiple entries depending on the available fuel types.  
        /// - Search is case-insensitive for all parameters.  
        ///
        /// </remarks>
        /// <param name="BrandName">Brand name of the station</param>
        /// <param name="Street">Street name where the station is located</param>
        /// <param name="HouseNumber">House number of the station</param>
        /// <param name="City">City where the station is located</param>
        /// <response code="200">Station profile successfully retrieved</response>
        /// <response code="400">Validation error — one or more required parameters are missing or empty</response>
        /// <response code="404">No matching station found</response>
        /// <response code="500">Server error — something went wrong while processing the request</response>
        [HttpGet("profile")]
        public async Task<IActionResult> GetStationProfileAsync([FromQuery] FindStationRequest request)
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
        /// The photo is uploaded to Azurite storage and a unique proposal record is created in the database.  
        /// The station is identified based on the provided data (brand name, street, house number, city).  
        ///
        /// Example request (multipart/form-data)  
        /// ```
        /// POST /api/price-proposal/add
        /// Content-Type: multipart/form-data
        ///
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
        /// - **BrandName**: Gas station brand name (e.g., Orlen, Shell, BP). Required.
        /// - **Street**: Street name where the station is located. Required.
        /// - **HouseNumber**: Street number of the station. Required.
        /// - **City**: City where the station is located. Required.
        /// - **FuelType**: Must be one of: `PB95`, `PB98`, `ON`, `LPG`, `E85`. Required.
        /// - **ProposedPrice**: Must be greater than 0, represents price in PLN per liter. Required.
        /// - **Photo**: Required, max size **5 MB**, allowed formats: **JPEG, JPG, PNG, WEBP**.
        ///
        /// Notes  
        /// - User email is automatically retrieved from JWT token claims.
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
        [EnableRateLimiting("upload")]
        [HttpPost("price-proposal/add")]
        public async Task<IActionResult> AddNewPriceProposalAsync([FromForm] AddNewPriceProposalRequest request)
        {
            var (email, error) = GetAuthenticatedUser();
            if (error != null) return error;

            var result = await _priceProposalServices.AddNewProposalAsync(email, request);
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
        /// Get all available fuel type codes.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns a list of all fuel type codes (identifiers) available in the system.  
        /// These codes can be used for filtering stations by fuel type in other endpoints.  
        ///
        /// Example response  
        /// ```json
        /// [
        ///   "PB95",
        ///   "PB98",
        ///   "ON",
        ///   "LPG",
        ///   "E85"
        /// ]
        /// ```
        ///
        /// Notes  
        /// - The response contains **unique fuel type codes** only.  
        /// - If no fuel types are found in the database, a `404` response is returned.  
        /// </remarks>
        /// <response code="200">Fuel type codes successfully retrieved</response>
        /// <response code="404">No fuel type codes found in the database</response>
        /// <response code="500">An unexpected server error occurred</response>
        [HttpGet("fuel-codes")]
        public async Task<IActionResult> GetAllFuelTypeCodesAsync()
        {
            var result = await _fuelTypeServices.GetAllFuelTypeCodesAsync();
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
        /// Get price proposals for a specific gas station.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns a paginated list of pending price proposals submitted by users for a specific gas station.  
        ///
        /// Station Identification  
        /// All parameters are **required** to uniquely identify a station:
        /// - **BrandName**: The gas station brand (e.g., "Shell", "BP", "Orlen")
        /// - **Street**: Street name where the station is located
        /// - **HouseNumber**: Building/house number of the station
        /// - **City**: City where the station is located
        ///
        /// Pagination  
        /// - **PageNumber**: Page number to retrieve (default: 1)
        /// - **PageSize**: Number of items per page (default: 10)
        ///
        /// Example request  
        /// ```
        /// GET /api/station/price-proposal?BrandName=Shell&amp;Street=aleja%20Aleksandra%20Brücknera&amp;HouseNumber=53&amp;City=Wrocław&amp;PageNumber=1&amp;PageSize=10
        /// ```
        ///
        /// Example response  
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": {
        ///     "items": [
        ///       {
        ///         "userName": "User4",
        ///         "fuelCode": "PB95",
        ///         "proposedPrice": 6.47,
        ///         "createdAt": "2025-10-28T17:21:39.810956Z"
        ///       },
        ///       {
        ///         "userName": "User5",
        ///         "fuelCode": "E85",
        ///         "proposedPrice": 6.71,
        ///         "createdAt": "2025-11-06T17:21:39.811112Z"
        ///       }
        ///     ],
        ///     "pageNumber": 1,
        ///     "pageSize": 10,
        ///     "totalCount": 5,
        ///     "totalPages": 1,
        ///     "hasPreviousPage": false,
        ///     "hasNextPage": false
        ///   },
        ///   "message": "price proposals retrieved successfully",
        ///   "statusCode": 200,
        ///   "errors": null
        /// }
        /// ```
        ///
        /// Notes  
        /// - If the station is not found, an empty result set is returned with `totalCount: 0`.
        /// - Price proposals include the username of the submitter, fuel type code, proposed price, and submission timestamp.
        /// </remarks>
        /// <param name="request">Station identification parameters (BrandName, Street, HouseNumber, City)</param>
        /// <param name="pagged">Pagination parameters (PageNumber, PageSize)</param>
        /// <response code="200">Price proposals successfully retrieved (may be empty if station not found)</response>
        /// <response code="500">An unexpected server error occurred</response>
        [HttpGet("price-proposal")]
        public async Task<IActionResult> GetPriceProposaByStationAsync([FromQuery] FindStationRequest request, [FromQuery] GetPaggedRequest pagged)
        {
            var result = await _stationServices.GetPriceProposalByStationAsync(request, pagged);
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
        /// Get fuel price history for a specific gas station.
        /// </summary>
        /// <remarks>
        /// Description  
        /// Returns the complete price history for all fuel types or a specific fuel type at a given gas station.  
        ///
        /// Station Identification  
        /// All parameters are **required** to uniquely identify a station:
        /// - **BrandName**: The gas station brand (e.g., "Shell", "BP", "Orlen")
        /// - **Street**: Street name where the station is located
        /// - **HouseNumber**: Building/house number of the station
        /// - **City**: City where the station is located
        ///
        /// Fuel Type Filter (Optional)  
        /// - **fuelCode**: Fuel type code (e.g., "PB95", "ON", "LPG"). If omitted, returns history for all fuel types available at the station.
        ///
        /// Example requests  
        /// ```
        /// # Get history for all fuels
        /// GET /api/station/fuel-price/history?BrandName=Shell&amp;Street=Marszałkowska&amp;HouseNumber=1&amp;City=Warszawa
        ///
        /// # Get history for specific fuel
        /// GET /api/station/fuel-price/history?BrandName=Shell&amp;Street=Marszałkowska&amp;HouseNumber=1&amp;City=Warszawa&amp;fuelCode=PB95
        /// ```
        ///
        /// Example response (single fuel)  
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": {
        ///     "fuelType": "Benzyna 95",
        ///     "fuelCode": "PB95",
        ///     "validFrom": [
        ///       "2024-10-05T12:00:00Z",
        ///       "2024-11-10T08:00:00Z",
        ///       "2024-12-20T10:30:00Z"
        ///     ],
        ///     "validTo": [
        ///       "2024-11-10T08:00:00Z",
        ///       "2024-12-20T10:30:00Z",
        ///       null
        ///     ],
        ///     "price": [
        ///       6.40,
        ///       6.50,
        ///       6.80
        ///     ]
        ///   },
        ///   "message": "Fuel price history retrieved successfully.",
        ///   "statusCode": 200,
        ///   "errors": null
        /// }
        /// ```
        ///
        /// Example response (all fuels)  
        /// ```json
        /// {
        ///   "isSuccess": true,
        ///   "data": [
        ///     {
        ///       "fuelType": "Benzyna 95",
        ///       "fuelCode": "PB95",
        ///       "validFrom": ["2024-10-05T12:00:00Z", "2024-12-20T10:30:00Z"],
        ///       "validTo": ["2024-12-20T10:30:00Z", null],
        ///       "price": [6.40, 6.80]
        ///     },
        ///     {
        ///       "fuelType": "Diesel",
        ///       "fuelCode": "ON",
        ///       "validFrom": ["2024-10-05T12:00:00Z"],
        ///       "validTo": [null],
        ///       "price": [6.20]
        ///     }
        ///   ],
        ///   "message": "Fuel price history retrieved successfully.",
        ///   "statusCode": 200,
        ///   "errors": null
        /// }
        /// ```
        ///
        /// Notes  
        /// - The arrays `validFrom`, `validTo`, and `price` have matching indices (e.g., `price[0]` corresponds to `validFrom[0]` and `validTo[0]`).
        /// - A `null` value in `validTo` indicates the current active price.
        /// - Prices are sorted chronologically from oldest to newest.
        /// - Perfect for generating price history charts and graphs.
        /// </remarks>
        /// <param name="findStation">Station identification parameters (BrandName, Street, HouseNumber, City)</param>
        /// <param name="fuelCode">Optional fuel type code to filter history for a specific fuel (e.g., "PB95", "ON")</param>
        /// <response code="200">Fuel price history successfully retrieved</response>
        /// <response code="400">Invalid fuel type code provided</response>
        /// <response code="404">Station not found or no price history available</response>
        /// <response code="500">An unexpected server error occurred</response>
        [HttpGet("fuel-price/history")]
        public async Task<IActionResult> GetFuelPriceHistoryAsync(
            [FromQuery] FindStationRequest findStation, 
            [FromQuery] string? fuelCode)
        {
            var result = await _stationServices.GetFuelPriceHistoryAsync(findStation, fuelCode);
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
