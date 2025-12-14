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
        private readonly IFuelTypeRepository _fuelTypeRepository;
        private readonly CacheService _cache;

        public StationServices(
            IStationRepository stationRepository,
           ILogger<StationServices> logger,
           IBrandRepository brandRepository,
           IFuelTypeRepository fuelTypeRepository,
           CacheService cache)
        {
            _stationRepository = stationRepository;
            _logger = logger;
            _brandRepository = brandRepository;
            _fuelTypeRepository = fuelTypeRepository;
            _cache = cache;
        }

        public async Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync(GetStationsRequest request)
        {
            try
            {
                var result = await _cache.GetOrSetAsync(
                    CacheService.CacheKeys.StationMap,
                    async () => await _stationRepository.GetAllStationsForMapAsync(request),
                    CacheService.CacheExpiry.Medium
                    );

                if (result == null || result.Count == 0)
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
                    result);

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
                    var fuelTypeCodes = await _fuelTypeRepository.GetAllFuelTypeCodesAsync();

                    var invalidFuelTypes = request.FuelType
                        .Where(fuelType => !fuelTypeCodes.Contains(fuelType, StringComparer.OrdinalIgnoreCase))
                        .ToList();

                    if (invalidFuelTypes.Any())
                    {
                        _logger.LogWarning("Invalid fuel type(s) provided: {FuelTypes}", string.Join(", ", invalidFuelTypes));

                        return Result<PagedResult<GetStationListResponse>>.Bad(
                            "Validation error",
                            StatusCodes.Status400BadRequest,
                            new List<string> { $"Invalid fuel type(s): {string.Join(", ", invalidFuelTypes)}" });
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

                if (request.PriceUpdatedAfter.HasValue && request.PriceUpdatedBefore.HasValue)
                {
                    if (request.PriceUpdatedAfter.Value > request.PriceUpdatedBefore.Value)
                    {
                        _logger.LogWarning("Invalid date range for price update filter");
                        return Result<PagedResult<GetStationListResponse>>.Bad(
                            "Validation error",
                            StatusCodes.Status400BadRequest,
                            new List<string> { "PriceUpdatedAfter cannot be later than PriceUpdatedBefore." });
                    }
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

        public async Task<Result<GetStationListResponse>> GetStationProfileAsync(FindStationRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.BrandName))
                {
                    _logger.LogWarning("Brand name cannot be empty");
                    return Result<GetStationListResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Brand name cannot be empty" }
                        );
                }

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

                var result = await _stationRepository.GetStationProfileAsync(request);

                if (result == null)
                {
                    _logger.LogWarning("Cannot find info about this station");
                    return Result<GetStationListResponse>.Bad(
                        "Server error",
                        StatusCodes.Status404NotFound,
                        new List<string> { "Cannot find info about this station" }
                        );
                }

                await _cache.InvalidateStationCacheAsync();

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

        public async Task<Result<PagedResult<GetStationListForAdminResponse>>> GetStationsListForAdminAsync(GetPaggedRequest pagged, TableRequest request)
            => await ((Func<Task<List<GetStationListForAdminResponse>>>)
            (() => _stationRepository.GetStationsListForAdminAsync(request)))
            .ToCachedPagedResultAsync(
                CacheService.CacheKeys.StationList,
                pagged,
                request,
                _cache,
                _logger,
                "stations",
                CacheService.CacheExpiry.Medium
                );

        public async Task<Result<bool>> EditStationAsync(EditStationRequest request)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(request.NewBrandName))
                {
                    bool isBrandExist = await _brandRepository.FindBrandAsync(request.NewBrandName);

                    if (!isBrandExist)
                    {
                        _logger.LogWarning("Invalid brand name: {BrandName}", request.NewBrandName);
                        return Result<bool>.Bad(
                            "Validation error",
                            StatusCodes.Status400BadRequest,
                            new List<string> { $"Invalid brand name: {request.NewBrandName}" },
                            false);
                    }
                }

                var result = await _stationRepository.EditStationAsync(request);

                if (!result)
                {
                    _logger.LogWarning("Failed to edit station details.");
                    return Result<bool>.Bad(
                        "Failed to edit station details.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Could not update the station with the provided details." },
                        false);
                }

                await _cache.InvalidateBrandCacheAsync();


                if (!string.IsNullOrWhiteSpace(request.NewBrandName)) await _cache.InvalidateBrandCacheAsync();

                await _cache.InvalidateFuelTypeCacheAsync();

                return Result<bool>.Good(
                    "Station details updated successfully.",
                    StatusCodes.Status200OK,
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while editing station details: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" },
                    false);
            }
        }

        public async Task<Result<GetStationInfoForEditResponse>> GetStationInfoForEdit(FindStationRequest request)
        {
            try
            {
                var cacheKey = _cache.GenerateCacheKey(
                    $"{CacheService.CacheKeys.StationPrefix}edit:{request.BrandName}:{request.City}:{request.Street}:{request.HouseNumber}"
                );

                var result = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () => await _stationRepository.GetStationInfoForEdit(request),
                    CacheService.CacheExpiry.Short
                );

                if (result == null)
                {
                    _logger.LogWarning("Station not found for editing.");
                    return Result<GetStationInfoForEditResponse>.Bad(
                        "Station not found.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No station found with the provided details." });
                }

                return Result<GetStationInfoForEditResponse>.Good(
                    "Station info retrieved successfully.",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving station info for edit: {ex.Message} | {ex.InnerException}");
                return Result<GetStationInfoForEditResponse>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<bool>> AddNewStationAsync(AddStationRequest request)
        {
            try
            {
                bool isBrandExist = await _brandRepository.FindBrandAsync(request.BrandName);

                if (!isBrandExist)
                {
                    _logger.LogWarning("Invalid brand name: {BrandName}", request.BrandName);
                    return Result<bool>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { $"Invalid brand name: {request.BrandName}" },
                        false);
                }

                if (request.FuelTypes == null || !request.FuelTypes.Any())
                {
                    return Result<bool>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "At least one fuel type must be provided." },
                        false);
                }

                var fuelTypeCodes = await _fuelTypeRepository.GetAllFuelTypeCodesAsync();
                var invalidFuelTypes = request.FuelTypes
                 .Where(ft => !fuelTypeCodes.Contains(ft.Code, StringComparer.OrdinalIgnoreCase))
                 .Select(ft => ft.Code)
                 .ToList();


                if (invalidFuelTypes.Any())
                {
                    _logger.LogWarning("Invalid fuel type(s) provided: {FuelTypes}", string.Join(", ", invalidFuelTypes));

                    return Result<bool>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { $"Invalid fuel type(s): {string.Join(", ", invalidFuelTypes)}" },
                        false);
                }

                var result = await _stationRepository.AddNewStationAsync(request);


                await _cache.InvalidateBrandCacheAsync();
                await _cache.InvalidateStationCacheAsync();
                await _cache.InvalidateFuelTypeCacheAsync();

                if (!result)
                {
                    _logger.LogWarning("Failed to add new station.");
                    return Result<bool>.Bad(
                        "Failed to add new station.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Could not add the station with the provided details." },
                        false);
                }

                return Result<bool>.Good(
                    "New station added successfully.",
                    StatusCodes.Status201Created,
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding new station: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" },
                    false);
            }
        }

        public async Task<Result<bool>> DeleteStationAsync(FindStationRequest request)
        {
            try
            {
                var result = await _stationRepository.DeleteStationAsync(request);
                if (!result)
                {
                    _logger.LogWarning("Failed to delete station.");
                    return Result<bool>.Bad(
                        "Failed to delete station.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Could not delete the station with the provided details." },
                        false);
                }


                await _cache.InvalidateBrandCacheAsync();
                await _cache.InvalidateStationCacheAsync();
                await _cache.InvalidateFuelTypeCacheAsync();

                return Result<bool>.Good(
                    "Station deleted successfully.",
                    StatusCodes.Status200OK,
                    true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting station: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" },
                    false);
            }
        }

        public async Task<Result<PagedResult<GetPriceProposalByStationResponse>>> GetPriceProposalByStationAsync(
            FindStationRequest request,
            GetPaggedRequest pagged)
        {
            string cacheKey = _cache.GenerateCacheKey(
                $"{CacheService.CacheKeys.PriceProposalByStation}{request.BrandName}:{request.City}:{request.Street}"
            );

            return await ((Func<Task<List<GetPriceProposalByStationResponse>>>)(() =>
                        _stationRepository.GetPriceProposaByStationAsync(request)))
                        .ToCachedPagedResultAsync(
                            cacheKey,
                            pagged,
                            _cache,
                            _logger,
                            "price proposals",
                            CacheService.CacheExpiry.Short
                        );
        }
    }
}
