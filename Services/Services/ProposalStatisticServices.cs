using Data.Interfaces;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
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
    public class ProposalStatisticServices : IProposalStatisticServices
    {
        private readonly IProposalStatisticRepository _proposalStatisticRepository;
        private readonly ILogger<ProposalStatisticServices> _logger;

        public ProposalStatisticServices(
            IProposalStatisticRepository proposalStatisticRepository,
            ILogger<ProposalStatisticServices> logger)
        {
            _proposalStatisticRepository = proposalStatisticRepository;
            _logger = logger;
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
                        StatusCodes.Status400BadRequest,
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
    }
}
