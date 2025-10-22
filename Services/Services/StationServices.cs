using Data.Interfaces;
using Data.Enums;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Services
{
    public class StationServices : IStationServices
    {
        private readonly IStationRepository _stationRepository;
        private readonly ILogger<StationServices> _logger;

        public StationServices(
            IStationRepository stationRepository,
           ILogger<StationServices> logger)
        {
            _stationRepository = stationRepository;
            _logger = logger;
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

        //public async Task<Result<PagedResult<GetStationListResponse>>> GetStationListAsync(GetStationListRequest request)
        //{
        //    try
        //    {
        //        if (request.Pagging.PageNumber.HasValue && request.Pagging.PageNumber < 1)
        //        {
        //            _logger.LogWarning("Invalid page number: {PageNumber}", request.Pagging.PageNumber);
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Invalid page number.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Page number must be greater than 0." });
        //        }

        //        if (request.Pagging.PageNumber.HasValue && (request.Pagging.PageSize < 1 || request.Pagging.PageSize > 100))
        //        {
        //            _logger.LogWarning("Invalid page size: {PageSize}", request.Pagging.PageSize);
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Invalid page size.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Page size must be between 1 and 100." });
        //        }

        //        if (request.LocationLatitude.HasValue && request.LocationLongitude.HasValue)
        //        {
        //            if (request.LocationLatitude < -90 || request.LocationLatitude > 90)
        //            {
        //                _logger.LogWarning("Invalid latitude value: {Latitude}", request.LocationLatitude);
        //                return Result<PagedResult<GetStationListResponse>>.Bad(
        //                    "Invalid latitude value.",
        //                    StatusCodes.Status400BadRequest,
        //                    new List<string> { "Latitude must be between -90 and 90." });
        //            }

        //            if (request.LocationLongitude < -180 || request.LocationLongitude > 180)
        //            {
        //                _logger.LogWarning("Invalid longitude value: {Longitude}", request.LocationLongitude);
        //                return Result<PagedResult<GetStationListResponse>>.Bad(
        //                    "Invalid longitude value.",
        //                    StatusCodes.Status400BadRequest,
        //                    new List<string> { "Longitude must be between -180 and 180." });
        //            }
        //        }

        //        if (request.FuelType != null && request.FuelType.Count > 0)
        //        {
        //            foreach (var fuelType in request.FuelType)
        //            {
        //                if (!Enum.TryParse<TypeOfFuel>(fuelType, true, out var _))
        //                {
        //                    _logger.LogWarning("Invalid fuel type value: {FuelType}", fuelType);
        //                    return Result<PagedResult<GetStationListResponse>>.Bad(
        //                        "Invalid fuel type value.",
        //                        StatusCodes.Status400BadRequest,
        //                        new List<string> { $"Fuel type '{fuelType}' is not recognized." });
        //                }
        //            }
        //        }

        //        if (request.MinPrice.HasValue && request.MinPrice < 0)
        //        {
        //            _logger.LogWarning("Invalid minimum price value: {MinPrice}", request.MinPrice);
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Invalid minimum price value.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Minimum price cannot be negative." });
        //        }

        //        if (request.MaxPrice.HasValue && request.MaxPrice < 0)
        //        {
        //            _logger.LogWarning("Invalid maximum price value: {MaxPrice}", request.MaxPrice);
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Invalid maximum price value.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Maximum price cannot be negative." });
        //        }

        //        if (!string.IsNullOrEmpty(request.BrandName))
        //        {
        //            bool isBrandExist = await _stationRepository.FindBrandAsync(request.BrandName);

        //            if (!isBrandExist)
        //            {
        //                _logger.LogWarning("Invalid brand name: {BrandName}", request.BrandName);
        //                return Result<PagedResult<GetStationListResponse>>.Bad(
        //                    "Validation error",
        //                    StatusCodes.Status400BadRequest,
        //                    new List<string> { $"Invalid brand name: {request.BrandName}" });
        //            }
        //        }

        //        if (request.SortingByPrice.HasValue && request.SortingByPrice.Value
        //            && request.SortingByDisance.HasValue && request.SortingByDisance.Value)
        //        {
        //            _logger.LogWarning("Conflicting sorting options: both SortingByPrice and SortingByDistance are set to true.");
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Conflicting sorting options.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Cannot sort by both price and distance simultaneously." });
        //        }

        //        if (request.SortingByDisance.HasValue && request.SortingByDisance.Value
        //            && (!request.LocationLatitude.HasValue || !request.LocationLongitude.HasValue))
        //        {
        //            _logger.LogWarning("Conflict: cannot sort by distance if Latitude and Longitude are null");
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Conflicting sorting options.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Cannot sort by distance if Latitude and Longitude are null." });
        //        }

        //        if (request.SortingByPrice.HasValue && request.SortingByPrice.Value
        //            && (request.FuelType == null || !request.FuelType.Any()))
        //        {
        //            _logger.LogWarning("Conflict: cannot sort by price if FuelType is null or empty");
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Conflicting sorting options.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Cannot sort by price if FuelType is null or empty." });
        //        }

        //        if ((request.FuelType == null || !request.FuelType.Any())
        //            && (request.MinPrice.HasValue || request.MaxPrice.HasValue))
        //        {
        //            _logger.LogWarning("Conflict: cannot filtered by price if FuelType is null or empty");
        //            return Result<PagedResult<GetStationListResponse>>.Bad(
        //                "Conflicting sorting options.",
        //                StatusCodes.Status400BadRequest,
        //                new List<string> { "Cannot filter by price if FuelType is null or empty." });
        //        }
        //        var result = await _stationRepository.GetStationListAsync(request);

        //        if (result == null || result.Count == 0)
        //        {
        //            _logger.LogWarning("No stations found in the database.");

        //            var emptyPage = new PagedResult<GetStationListResponse>
        //            {
        //                Items = new List<GetStationListResponse>(),
        //                PageNumber = request.Pagging.PageNumber ?? 0,
        //                PageSize = request.Pagging.PageSize ?? 0,
        //                TotalCount = 0,
        //                TotalPages = 0
        //            };

        //            return Result<PagedResult<GetStationListResponse>>.Good(
        //                "No stations found matching the criteria.",
        //                StatusCodes.Status200OK,
        //                emptyPage);
        //        }

        //        if (!request.Pagging.PageSize.HasValue) request.Pagging.PageSize = 10;
        //        if (!request.Pagging.PageNumber.HasValue) request.Pagging.PageNumber = 1;

        //        var pagedResult = result.ToPagedResult(request.Pagging.PageNumber.Value, request.Pagging.PageSize.Value);

        //        if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
        //        {
        //            pagedResult = result.ToPagedResult(pagedResult.TotalPages, request.Pagging.PageSize.Value);
        //        }

        //        return Result<PagedResult<GetStationListResponse>>.Good(
        //            "Stations retrieved successfully.",
        //            StatusCodes.Status200OK,
        //            pagedResult);
        //    }
        //    catch (Exception ex)
        //    {
        //        _logger.LogError(ex, $"An error occurred while retrieving station list: {ex.Message} | {ex.InnerException}");
        //        return Result<PagedResult<GetStationListResponse>>.Bad(
        //            "An error occurred while processing your request.",
        //            StatusCodes.Status500InternalServerError,
        //            new List<string> { $"{ex.Message} | {ex.InnerException}" });
        //    }
        //}

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
                    bool isBrandExist = await _stationRepository.FindBrandAsync(request.BrandName);

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
    }
}
