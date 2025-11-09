using Data.Interfaces;
using DTO.Requests;
using DTO.Responses;
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
        {
            try
            {
                var result = await _fuelTypeRepository.GetFuelsTypeListAsync(request);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No brands found in the database.");

                    var emptyPaged = new PagedResult<GetFuelTypeResponses>
                    {
                        Items = new List<GetFuelTypeResponses>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<GetFuelTypeResponses>>.Good(
                        "No brands found in the database",
                        StatusCodes.Status200OK,
                        emptyPaged
                    );
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<GetFuelTypeResponses>>.Good(
                    "Brands retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving fuel types: {ex.Message} | {ex.InnerException}");
                return Result<PagedResult<GetFuelTypeResponses>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

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

    }
}
