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

        public async Task<Result<List<GetAllStationsForMap>>> GetAllStationsForMapAsync()
        {
            try
            {
                var resutlt = await _stationRepository.GetAllStationsForMapAsync();

                if (resutlt == null)
                {
                    _logger.LogWarning("No stations found in the database.");
                    return Result<List<GetAllStationsForMap>>.Bad(
                        "No stations found.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No stations available in the database." });
                }


                return Result<List<GetAllStationsForMap>>.Good(
                    "Stations retrieved successfully.",
                    StatusCodes.Status200OK,
                    resutlt);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex ,$"An error occurred while retrieving stations: {ex.Message} | {ex.InnerException}");
                return Result<List<GetAllStationsForMap>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    
    }
}
