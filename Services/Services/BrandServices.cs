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
            => await _brandRepository.GetBrandToListAsync(request).ToPagedResultAsync(pagged, _logger, "brand");

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
                _logger.LogError(ex, $"An error occurred while retrieving brand: {ex.Message} | {ex.InnerException}");
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

                bool isOldExist = await _brandRepository.FindBrandAsync(oldName);

                if (!isOldExist)
                {
                    _logger.LogWarning("This brand dosn't exist {OldName}", oldName);
                    return Result<bool>.Bad(
                        "Application error",
                        StatusCodes.Status409Conflict,
                        new List<string> { "This brand dosn't exist" }
                        );
                }

                bool isNewExist = await _brandRepository.FindBrandAsync(newName);

                if (isNewExist)
                { 
                    _logger.LogWarning("Brand {NewName} already exists", newName);
                    return Result<bool>.Bad(
                        "Appliaction Error",
                        StatusCodes.Status409Conflict,
                        new List<string> { "Brand already exists" }
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
                _logger.LogError(ex, $"An error occurred while editing brand: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                        "An error occurred while processing your request.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<bool>> AddBrandAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Validation error. Name is null, empyt white space");

                    return Result<bool>.Bad(
                        "Valiadtion error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Name is null, empyt white space" }
                        );
                }

                var isBrandExist = await _brandRepository.FindBrandAsync(name);

                if (isBrandExist)
                {
                    _logger.LogWarning("Brand {Name} already exists", name);
                    return Result<bool>.Bad(
                        "Appliaction Error",
                        StatusCodes.Status409Conflict,
                        new List<string> { "Brand already exists" }
                        );
                }

                var result = await _brandRepository.AddBrandAsync(name);

                if (!result)
                {
                    _logger.LogError("Server error. Cannot add Brand");
                    return Result<bool>.Bad(
                       "Server error",
                       StatusCodes.Status500InternalServerError,
                       new List<string> { "Cannot add Brand" }
                       );
                }

                return Result<bool>.Good(
                       $"Brand {name} add successfull",
                       StatusCodes.Status201Created,
                       result
                       );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while add brand: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                        "An error occurred while processing your request.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<bool>> DeleteBrandAsync(string name)
        {
            try
            {
                if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(name))
                {
                    _logger.LogWarning("Validation error. Name is null, empyt white space");
                    return Result<bool>.Bad(
                        "Valiadtion error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Name is null, empyt white space" }
                        );
                }

                var isBrandExist = await _brandRepository.FindBrandAsync(name);

                if (!isBrandExist)
                {
                    _logger.LogWarning("Brand {Name} not found", name);
                    return Result<bool>.Bad(
                        "Appliaction Error",
                        StatusCodes.Status404NotFound,
                        new List<string> { "Brand not found" }
                        );
                }

                var result = await _brandRepository.DeleteBrandAsync(name);

                if (!result)
                {
                    _logger.LogError("Server error. Cannot delete Brand");
                    return Result<bool>.Bad(
                       "Server error",
                       StatusCodes.Status500InternalServerError,
                       new List<string> { "Cannot delete Brand" }
                       );
                }

                return Result<bool>.Good(
                       $"Brand {name} delete successfull",
                       StatusCodes.Status200OK,
                       result
                       );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while delete brand: {ex.Message} | {ex.InnerException}");
                return Result<bool>.Bad(
                        "An error occurred while processing your request.",
                        StatusCodes.Status404NotFound,
                        new List<string> { $"{ex.Message} | {ex.InnerException}" });

            }
        }
    }
}
