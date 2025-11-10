using Data.Interfaces;
using Data.Reopsitories;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class FuelTypeServices : IFuelTypeServices
    {
        private readonly IFuelTypeRepository _fuelTypeRepository;
        private readonly ILogger<FuelTypeServices> _logger;
        public FuelTypeServices(
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

        public async Task<Result<PagedResult<GetFuelTypeResponses>>> GetFuelsTypeListAsync(GetPaggedRequest pagged, TableRequest request)
            => await _fuelTypeRepository.GetFuelsTypeListAsync(request).ToPagedResultAsync(pagged, _logger, "fuel types");

        public async Task<Result<bool>> AddFuelTypeAsync(AddFuelTypeRequest request)
        {
            try
            {
                string code = (request.Code ?? string.Empty).Replace(" ", "").ToUpperInvariant();
                var isExist = await _fuelTypeRepository.FindFuelTypeByCodeAsync(code);

                if (isExist != null)
                {
                    _logger.LogWarning($"Fuel type with code {code} already exists.");
                    return Result<bool>.Bad(
                        "Fuel type already exists.",
                        StatusCodes.Status409Conflict,
                        new List<string> { $"Fuel type with code {code} already exists." }
                        );
                }

                if (string.IsNullOrWhiteSpace(request.Name))
                {
                    _logger.LogWarning("Fuel name is null or white spaces");
                    return Result<bool>.Bad(
                        "ValidationError",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "IsNullOrWhiteSpaces" }
                        );
                }

                var words = request.Name
                    .Trim()
                    .ToLower()
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Where(w => w.Length > 0)
                    .Select(w => char.ToUpper(w[0]) + w.Substring(1));

                var name = string.Join(' ', words);

                var response = await _fuelTypeRepository.AddFuelTypeAsync(name, code);

                if (!response)
                {
                    _logger.LogError("Failed to add fuel type to the database.");
                    return Result<bool>.Bad(
                        "Failed to add fuel type.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "An error occurred while adding the fuel type to the database." }
                        );
                }

                return Result<bool>.Good(
                    "Fuel type added successfully.",
                    StatusCodes.Status201Created,
                    true
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while adding fuel type: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<bool>> EditFuelTypeAsync(EditFuelTypeRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.OldCode))
                {
                    _logger.LogWarning("Old fuel code is null or white spaces");
                    return Result<bool>.Bad(
                        "ValidationError",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "OldCodeIsNullOrWhiteSpaces" }
                    );
                }

                if (string.IsNullOrEmpty(request.NewName) && string.IsNullOrEmpty(request.NewCode))
                {
                    _logger.LogWarning("No new values provided for update");
                    return Result<bool>.Bad(
                        "ValidationError",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "At least one of NewName or NewCode must be provided." }
                    );
                }

                var existingFuelType = await _fuelTypeRepository.FindFuelTypeByCodeAsync(request.OldCode);
                if (existingFuelType == null)
                {
                    _logger.LogWarning("Fuel type with code {code} does not exist.", request.OldCode);
                    return Result<bool>.Bad(
                        "Fuel type does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"Fuel type with code {request.OldCode} does not exist." }
                    );
                }

                string? newCode = request.NewCode?.Replace(" ", "").ToUpperInvariant();
                string? newName = request.NewName != null
                    ? string.Join(' ',
                        request.NewName
                            .Trim()
                            .ToLower()
                            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                            .Select(w => char.ToUpper(w[0]) + w.Substring(1)))
                    : null;

                if (!string.IsNullOrEmpty(newCode) && newCode != existingFuelType.Code)
                {
                    var codeTaken = await _fuelTypeRepository.FindFuelTypeByCodeAsync(newCode);
                    if (codeTaken != null)
                    {
                        _logger.LogWarning("Fuel type with code {code} already exists.", newCode);
                        return Result<bool>.Bad(
                            "Fuel type already exists.",
                            StatusCodes.Status409Conflict,
                            new List<string> { $"Fuel type with code {newCode} already exists." }
                        );
                    }
                }

                var result = await _fuelTypeRepository.EditFuelTypeAsync(existingFuelType, newName, newCode);

                if (!result)
                {
                    _logger.LogError("Failed to edit fuel type in the database.");
                    return Result<bool>.Bad(
                        "Failed to edit fuel type.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "An error occurred while editing the fuel type in the database." }
                    );
                }

                return Result<bool>.Good(
                    "Fuel type edited successfully.",
                    StatusCodes.Status200OK,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while editing fuel type: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" }
                );
            }
        }

        public async Task<Result<bool>> DeleteFuelTypeAsync(string code)
        {
            try
            {
                if (string.IsNullOrEmpty(code))
                {
                    _logger.LogWarning("Fuel code is null or empty");
                    return Result<bool>.Bad(
                        "ValidationError",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "CodeIsNullOrEmpty" }
                    );
                }

                var fuelType = await _fuelTypeRepository.FindFuelTypeByCodeAsync(code);

                if (fuelType == null)
                {
                    _logger.LogWarning("Fuel type with code {code} does not exist.", code);
                    return Result<bool>.Bad(
                        "Fuel type does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"Fuel type with code {code} does not exist." }
                    );
                }

                var result = await _fuelTypeRepository.DeleteFuelTypeAsync(fuelType);

                if (!result)
                {
                    _logger.LogError("Cannot Delete This Fuel Type");
                    return Result<bool>.Bad(
                        "Cannot Delete This Fuel Type.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { $"InternalServerError." }
                    );
                }

                return Result<bool>.Good(
                    "Fuel type deleted successfully.",
                    StatusCodes.Status200OK,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while deleting fuel type: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" }
                );
            }
        }
    }
}
