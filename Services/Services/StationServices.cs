using Data.Enums;
using Data.Interfaces;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class StationServices : IStationServices
    {
        private readonly IStationRepository _stationRepository;
        private readonly ILogger<StationServices> _logger;
        private readonly IBrandRepository _brandRepository;

        public StationServices(
            IStationRepository stationRepository,
           ILogger<StationServices> logger,
           IBrandRepository brandRepository)
        {
            _stationRepository = stationRepository;
            _logger = logger;
            _brandRepository = brandRepository;
        }

        public async Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync(GetStationsRequest request)
        {
            try
            {
                var resutlt = await _stationRepository.GetAllStationsForMapAsync(request);

                if (resutlt == null || resutlt.Count == 0)
                {
                    _logger.LogWarning("No stations found in the database.");
                    return Result<List<GetStationsResponse>>.Bad(
                        "No stations found.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No stations available in the database." });
                }


                return Result<List<GetStationsResponse>>.Good(
                    "Stations retrieved successfully.",
                    StatusCodes.Status200OK,
                    resutlt);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving stations: {ex.Message} | {ex.InnerException}");
                return Result<List<GetStationsResponse>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<List<GetStationsResponse>>> GetNearestStationAsync(
            double latitude,
            double longitude,
            int? count
            )
        {
            try
            {

                if (latitude < -90 || latitude > 90)
                {
                    _logger.LogWarning("Invalid latitude value: {Latitude}", latitude);
                    return Result<List<GetStationsResponse>>.Bad(
                        "Invalid latitude value.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Latitude must be between -90 and 90." });
                }

                if (longitude < -180 || longitude > 180)
                {
                    _logger.LogWarning("Invalid longitude value: {Longitude}", longitude);
                    return Result<List<GetStationsResponse>>.Bad(
                        "Invalid longitude value.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Longitude must be between -180 and 180." });
                }

                var result = await _stationRepository.GetNearestStationAsync(latitude, longitude, count);

                if (result == null || result.Count == 0)
                {
                    _logger.LogWarning("No nearby stations found for the provided coordinates: ({Latitude}, {Longitude})", latitude, longitude);
                    return Result<List<GetStationsResponse>>.Bad(
                        "No nearby stations found.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No stations found near the provided location." });
                }

                return Result<List<GetStationsResponse>>.Good(
                    "Nearest stations retrieved successfully.",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving nearest stations: {ex.Message} | {ex.InnerException}");
                return Result<List<GetStationsResponse>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<PagedResult<GetStationListResponse>>> GetStationListAsync(GetStationListRequest request)
        {
            try
            {
                if (request.FuelType?.Any() == true)
                {
                    foreach (var fuelType in request.FuelType)
                    {
                        if (!Enum.TryParse<TypeOfFuel>(fuelType, true, out _))
                        {
                            _logger.LogWarning("Invalid fuel type {FuelType}", fuelType);
                            return Result<PagedResult<GetStationListResponse>>.Bad(
                                "Invalid fuel tyep",
                                StatusCodes.Status400BadRequest,
                                new List<string> { $"Fuel type '{fuelType}' is not recoginized" }
                                );
                        }
                    }
                }

                if (!string.IsNullOrEmpty(request.BrandName))
                {
                    bool isBrandExist = await _brandRepository.FindBrandAsync(request.BrandName);

                    if (!isBrandExist)
                    {
                        _logger.LogWarning("Invalid brand name: {BrandName}", request.BrandName);
                        return Result<PagedResult<GetStationListResponse>>.Bad(
                            "Validation error",
                            StatusCodes.Status400BadRequest,
                            new List<string> { $"Invalid brand name: {request.BrandName}" });
                    }
                }

                if (request.SortingByDisance == true && request.SortingByPrice == true)
                {
                    _logger.LogWarning("Conflicting sorting options");
                    return Result<PagedResult<GetStationListResponse>>.Bad(
                        "Conflicting sorting options.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Cannot sort by both price and distance simultaneously." });
                }

                if (request.SortingByDisance.HasValue && request.SortingByDisance.Value
                    && (!request.LocationLatitude.HasValue || !request.LocationLongitude.HasValue))
                {
                    _logger.LogWarning("Cannot sort by distance without location");
                    return Result<PagedResult<GetStationListResponse>>.Bad(
                        "Missing required fields.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Cannot sort by distance without location coordinates." });
                }

                if (request.SortingByPrice.HasValue && request.SortingByPrice.Value
                    && (request.FuelType == null || !request.FuelType.Any()))
                {
                    _logger.LogWarning("Cannot sort by price without fuel type");
                    return Result<PagedResult<GetStationListResponse>>.Bad(
                        "Missing required fields.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Cannot sort by price without specifying fuel type." });
                }

                if ((request.FuelType == null || !request.FuelType.Any()) &&
                    (request.MinPrice.HasValue || request.MaxPrice.HasValue))
                {
                    _logger.LogWarning("Cannot filter by price without fuel type");
                    return Result<PagedResult<GetStationListResponse>>.Bad(
                        "Missing required fields.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Cannot filter by price without specifying fuel type." });
                }

                var result = await _stationRepository.GetStationListAsync(request);

                if (result == null || result.Count == 0)
                {
                    _logger.LogWarning("No stations found in the database.");

                    var emptyPage = new PagedResult<GetStationListResponse>
                    {
                        Items = new List<GetStationListResponse>(),
                        PageNumber = request.Pagging.PageNumber ?? 0,
                        PageSize = request.Pagging.PageSize ?? 0,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<GetStationListResponse>>.Good(
                        "No stations found matching the criteria.",
                        StatusCodes.Status200OK,
                        emptyPage);
                }

                int pageNumber = request.Pagging?.PageNumber ?? 1;
                int pageSize = request.Pagging?.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<GetStationListResponse>>.Good(
                    "Stations retrieved successfully.",
                    StatusCodes.Status200OK,
                    pagedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving station list: {ex.Message} | {ex.InnerException}");
                return Result<PagedResult<GetStationListResponse>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });

            }
        }

        public async Task<Result<GetStationListResponse>> GetStationProfileAsync(GetStationProfileRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.Street))
                {
                    _logger.LogWarning("Street cannot be empty");
                    return Result<GetStationListResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Street cannot be empty" }
                        );
                }

                if (string.IsNullOrEmpty(request.HouseNumber))
                {
                    _logger.LogWarning("House number cannot be empty");
                    return Result<GetStationListResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "House Number cannot be empty" }
                        );
                }

                if (string.IsNullOrEmpty(request.City))
                {
                    _logger.LogWarning("City cannot be empty");
                    return Result<GetStationListResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "City cannot be empty" }
                        );
                }

                if (string.IsNullOrEmpty(request.PostalCode))
                {
                    _logger.LogWarning("Postal code cannot be empty");
                    return Result<GetStationListResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Postal code cannot be empty" }
                        );
                }

                var result = await _stationRepository.GetStationProfileAsync(request);

                if (request == null)
                {
                    _logger.LogWarning("Cannot find info about this station");
                    return Result<GetStationListResponse>.Bad(
                        "Server error",
                        StatusCodes.Status404NotFound,
                        new List<string> { "Cannot find info about this station" }
                        );
                }

                return Result<GetStationListResponse>.Good(
                    "Stations retrieved successfully",
                    StatusCodes.Status200OK,
                    result
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving station profile: {ex.Message} | {ex.InnerException}");
                return Result<GetStationListResponse>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    }
}
