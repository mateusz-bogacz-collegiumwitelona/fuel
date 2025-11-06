using Data.Enums;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;
using System.Diagnostics;

namespace Services.Services
{
    public class PriceProposalServices : IPriceProposalServices
    {
        private readonly IPriceProposalRepository _priceProposalRepository;
        private readonly ILogger<PriceProposalServices> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStationRepository _stationRepository;
        private readonly IFuelTypeRepository _fuelTypeRepository;

        public PriceProposalServices(
            IPriceProposalRepository priceProposalRepository,
            ILogger<PriceProposalServices> logger,
            UserManager<ApplicationUser> userManager,
            IStationRepository stationRepository,
            IFuelTypeRepository fuelTypeRepository)
        {
            _priceProposalRepository = priceProposalRepository;
            _logger = logger;
            _userManager = userManager;
            _stationRepository = stationRepository;
            _fuelTypeRepository = fuelTypeRepository;
        }

        public async Task<Result<string>> AddNewProposalAsync(string email, AddNewPriceProposalRequest request)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                string extension = Path.GetExtension(request.Photo.FileName);

                if (!ContentConstants.FILE_TYPE_CONST.Contains(extension, StringComparer.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Uploaded photo has an invalid file type: {FileType}", extension);
                    return Result<string>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Invalid photo file type. Allowed types are: JPEG, JPG, PNG, WEBP." }
                        );
                }

                var vuelTypeCodes = await _fuelTypeRepository.GetAllFuelTypeCodesAsync();
                
                foreach (var code in vuelTypeCodes)
                {
                    if (string.Equals(code, request.FuelType, StringComparison.OrdinalIgnoreCase))
                    {
                        request.FuelType = code;
                        break;
                    }

                    if (!vuelTypeCodes.Contains(request.FuelType))
                    {
                        _logger.LogWarning("Invalid fuel type provided: {FuelType}", request.FuelType);
                        return Result<string>.Bad(
                            "Validation error",
                            StatusCodes.Status400BadRequest,
                            new List<string> { "Invalid fuel type provided." }
                            );
                    }
                }

                var fuelType = await _fuelTypeRepository.FindFuelTypeByNameAsync(request.FuelType);

                if (request.Photo.Length > ContentConstants.FILE_SIZE_CONST)
                {
                    _logger.LogWarning("Uploaded photo exceeds maximum allowed size. Size: {PhotoSize} bytes", request.Photo.Length);
                    return Result<string>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { $"Photo size exceeds the maximum allowed size of {ContentConstants.FILE_SIZE_CONST / (1024 * 1024)} MB." }
                        );
                }

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found.", email);
                    return Result<string>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "User not found with the provided email." }
                        );
                }

                var station = await _stationRepository.FindStationByDataAsync(
                    request.BrandName,
                    request.Street,
                    request.HouseNumber,
                    request.City
                    );

                if (station == null)
                {
                    _logger.LogWarning("Station not found for the provided data.");
                    return Result<string>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Station not found with the provided data." }
                        );
                }

                var isAdded = await _priceProposalRepository.AddNewPriceProposalAsync(
                    user,
                    station,
                    fuelType,
                    request.ProposedPrice,
                    request.Photo,
                    extension
                    );

                if (!isAdded)
                {
                    _logger.LogError("Failed to add new price proposal for user {UserId} at station {StationId}", user.Id, station.Id);
                    return Result<string>.Bad(
                        "Failed to add price proposal",
                        StatusCodes.Status500InternalServerError
                        );
                }

                stopwatch.Stop();
                _logger.LogInformation("New price proposal added successfully for user {UserId} at station {StationId} in {ElapsedMilliseconds} ms", user.Id, station.Id, stopwatch.ElapsedMilliseconds);
                return Result<string>.Good(
                    "Price proposal added successfully",
                    StatusCodes.Status200OK,
                    $"Price proposal added successfully in {stopwatch.ElapsedMilliseconds}"
                    );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while add new price proposal: {ex.Message} | {ex.InnerException}");
                return Result<string>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<GetPriceProposalResponse>> GetPriceProposal(string photoToken)
        {
            try
            {
                if (string.IsNullOrEmpty(photoToken))
                {
                    _logger.LogWarning("Photo token is null or empty.");
                    return Result<GetPriceProposalResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Photo token cannot be null or empty." }
                        );
                }

                var response = await _priceProposalRepository.GetPriceProposal(photoToken);

                if (response == null)
                {
                    _logger.LogWarning("Price proposal with photo token {PhotoToken} not found.", photoToken);
                    return Result<GetPriceProposalResponse>.Bad(
                        "Not found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "Price proposal not found with the provided photo token." }
                        );
                }

                return Result<GetPriceProposalResponse>.Good(
                    "Price proposal retrieved successfully",
                    StatusCodes.Status200OK,
                    response
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving price proposal with photo token {photoToken}: {ex.Message} | {ex.InnerException}");
                return Result<GetPriceProposalResponse>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    }
}
