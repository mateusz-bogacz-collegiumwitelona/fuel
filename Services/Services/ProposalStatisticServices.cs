using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class ProposalStatisticServices : IProposalStatisticServices
    {
        private readonly IProposalStatisticRepository _proposalStatisticRepository;
        private readonly ILogger<ProposalStatisticServices> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        public ProposalStatisticServices(
            IProposalStatisticRepository proposalStatisticRepository,
            ILogger<ProposalStatisticServices> logger,
            UserManager<ApplicationUser> userManager
            )
        {
            _proposalStatisticRepository = proposalStatisticRepository;
            _logger = logger;
            _userManager = userManager;
        }
        public async Task<Result<GetProposalStatisticResponse>> GetUserProposalStatisticResponse(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is required to fetch user proposal statistics.");
                    return Result<GetProposalStatisticResponse>.Bad(
                        "Validation Error",
                        StatusCodes.Status401Unauthorized,
                        new List<string> { "Email is required" }
                    );
                }

                var response = await _proposalStatisticRepository.GetUserProposalStatisticAsync(email);

                if (response == null)
                {
                    _logger.LogWarning("No proposal statistics found for user with email {Email}.", email);
                    return Result<GetProposalStatisticResponse>.Bad(
                        "Not Found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "User proposal statistic not found" }
                    );
                }
                return Result<GetProposalStatisticResponse>.Good(
                    "User proposal statistic retrieved successfully",
                    StatusCodes.Status200OK,
                    response
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while fetching proposal statistics for email: {email}",
                    email);

                return Result<GetProposalStatisticResponse>.Bad(
                    "Internal Server Error",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<PagedResult<TopUserResponse>>> GetTopUserListAsync(GetPaggedRequest request)
            => await _proposalStatisticRepository.GetTopUserListAsync().ToPagedResultAsync(request, _logger, "top users");

        public async Task<Result<bool>> UpdateTotalProposalsAsync(bool proposial, string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Email is required to update user proposal statistics.");
                    return Result<bool>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required" }
                    );
                }

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found.", email);
                    return Result<bool>.Bad(
                        "User Not Found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "User does not exist" }
                    );
                }

                var isUpdated = await _proposalStatisticRepository.UpdateTotalProposalsAsync(proposial, user.Id);

                if (!isUpdated)
                {
                    _logger.LogWarning("Failed to update proposal statistics for user with email {Email}.", email);
                    return Result<bool>.Bad(
                        "Update Failed",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Failed to update user proposal statistic" }
                    );
                }

                return Result<bool>.Good(
                    "User proposal statistic updated successfully",
                    StatusCodes.Status200OK,
                    true
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while updating proposal statistics for email: {email}",
                    email);
                return Result<bool>.Bad(
                    "Internal Server Error",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
