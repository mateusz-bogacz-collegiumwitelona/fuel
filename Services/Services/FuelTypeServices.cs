using Data.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class FuelTypeServices : IFuelTypeServices
    {
        private readonly IFuelTypeRepository _fuelTypeRepository;
        private readonly ILogger<FuelTypeServices> _logger;
        public FuelTypeServices (
            IFuelTypeRepository fuelTypeRepository,
            ILogger<FuelTypeServices> logger)
        {
            _fuelTypeRepository = fuelTypeRepository;
            _logger = logger;
        }

        public async Task<Result<List<string>>> GetAllFuelTypeCodesAsync()
        {
            try
            {
                var result = await _fuelTypeRepository.GetAllFuelTypeCodesAsync();

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No fuel code found");
                    return Result<List<string>>.Bad(
                        "No fuel code found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No fuel code available in the database." }
                        );
                }

                return Result<List<string>>.Good(
                    "Fuel code retrieved successfully.",
                    StatusCodes.Status200OK,
                    result
                    );
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving stations: {ex.Message} | {ex.InnerException}");
                return Result<List<string>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

    }
}
