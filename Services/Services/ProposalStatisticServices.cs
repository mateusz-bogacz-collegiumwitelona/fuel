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
        private readonly CacheService _cache;
        public ProposalStatisticServices(
            IProposalStatisticRepository proposalStatisticRepository,
            ILogger<ProposalStatisticServices> logger,
            UserManager<ApplicationUser> userManager,
            CacheService cache
            )
        {
            _proposalStatisticRepository = proposalStatisticRepository;
            _logger = logger;
            _userManager = userManager;
            _cache = cache;
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

                var user = await _userManager.FindByEmailAsync(email);

                var cacheKey = $"{CacheService.CacheKeys.UserStatsPrefix}{email}";

                var response = await _cache.GetOrSetAsync(
                    cacheKey,
                    async () => await _proposalStatisticRepository.GetUserProposalStatisticAsync(user),
                    CacheService.CacheExpiry.Short
                    );


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

        public async Task<Result<PagedResult<TopUserResponse>>> GetTopUserListAsync(GetPaggedRequest pagged)
            => await ((Func<Task<List<TopUserResponse>>>)
            (() => _proposalStatisticRepository.GetTopUserListAsync()))
            .ToCachedPagedResultAsync(
                CacheService.CacheKeys.TopUsers,
                pagged,
                _cache,
                _logger,
                "top users",
                CacheService.CacheExpiry.Short
                );

    }
}
