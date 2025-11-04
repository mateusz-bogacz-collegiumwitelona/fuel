using Data.Interfaces;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class BrandServices : IBrandServices
    {
        private readonly IBrandRepository _brandRepository;
        private readonly ILogger<BrandServices> _logger;

        public BrandServices(
            IBrandRepository brandRepository,
            ILogger<BrandServices> logger
            )
        {
            _brandRepository = brandRepository;
            _logger = logger;
        }

        public async Task<Result<PagedResult<GetBrandDataResponse>>> GetBrandToListAsync(GetPaggedRequest pagged, TableRequest request)
        {
            try
            {
                var result = await _brandRepository.GetBrandToListAsync(request);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No brands found in the database.");

                    var emptyPaged = new PagedResult<GetBrandDataResponse>
                    {
                        Items = new List<GetBrandDataResponse>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<GetBrandDataResponse>>.Good(
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

                return Result<PagedResult<GetBrandDataResponse>>.Good(
                    "Brands retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving brands.");
                return Result<PagedResult<GetBrandDataResponse>>.Bad(
                    "An error occurred while retrieving brands",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
        public async Task<Result<List<string>>> GetAllBrandsAsync()
        {
            try
            {
                var result = await _brandRepository.GetAllBrandsAsync();

                if (result == null || result.Count == 0)
                    return Result<List<string>>.Bad(
                        "No brands found.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "No brands available in the database." });


                return Result<List<string>>.Good(
                    $"Succes: {result.Count} is listed",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving station list: {ex.Message} | {ex.InnerException}");
                return Result<List<string>>.Bad(
                        "An error occurred while processing your request.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
        public async Task<Result<bool>> EditBrandAsync(string oldName, string newName)
        {
            try
            {
                if (string.IsNullOrEmpty(oldName) || string.IsNullOrWhiteSpace(oldName))
                {
                    _logger.LogWarning("Validation error. OldName is null, empyt white space");

                    return Result<bool>.Bad(
                        "Valiadtion error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "OldName is null, empyt white space" }
                        );
                }

                if (string.IsNullOrEmpty(newName) || string.IsNullOrWhiteSpace(newName))
                {
                    _logger.LogWarning("Validation error. NewName is null, empyt white space");

                    return Result<bool>.Bad(
                        "Valiadtion error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "NewName is null, empyt white space" }
                        );
                }

                var result = await _brandRepository.EditBrandAsync(oldName, newName);

                if (!result)
                {
                    _logger.LogError("Server error. Cannot edit Brand");
                    return Result<bool>.Bad(
                       "Server error",
                       StatusCodes.Status500InternalServerError,
                       new List<string> { "Cannot edit Brand" }
                       );
                }

                return Result<bool>.Good(
                       $"Brand {oldName} edited successfull",
                       StatusCodes.Status200OK,
                       result
                       );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while editing station: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                        "An error occurred while processing your request.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    }
}
