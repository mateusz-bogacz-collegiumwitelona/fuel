using Data.Interfaces;
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

        public async Task<Result<List<GetStationsResponse>>> GetAllStationsForMapAsync()
        {
            try
            {
                var resutlt = await _stationRepository.GetAllStationsForMapAsync();

                if (resutlt == null)
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
    }
}
