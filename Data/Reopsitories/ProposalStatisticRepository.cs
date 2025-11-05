using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

namespace Data.Reopsitories
{
    public class ProposalStatisticRepository : IProposalStatisticRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ProposalStatisticRepository> _logger;

        public ProposalStatisticRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProposalStatisticRepository> logger
            )
        {
            _context = context;
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found.", email);
                return null;
            }

            var proposals = _context.ProposalStatistics
                .FirstOrDefault(ps => ps.UserId == user.Id);

            if (proposals == null)
            {
                _logger.LogWarning("No proposal statistics found for user with email {Email}.", email);
                return null;
            }

            return new GetProposalStatisticResponse
            {
                TotalProposals = proposals.TotalProposals ?? 0,
                ApprovedProposals = proposals.ApprovedProposals ?? 0,
                RejectedProposals = proposals.RejectedProposals ?? 0,
                AcceptedRate = proposals.AcceptedRate ?? 0,
                Points = proposals.Points ?? 0,
                UpdatedAt = proposals.UpdatedAt
            };
        }

        public async Task<bool> AddProposalStatisticRecordAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                _logger.LogWarning("User with email {Email} not found.", email);
                return false;
            }

            var proposal = new ProposalStatistic
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                TotalProposals = 0,
                ApprovedProposals = 0,
                RejectedProposals = 0,
                AcceptedRate = 0,
                UpdatedAt = DateTime.UtcNow
            };

            await _context.ProposalStatistics.AddAsync(proposal);
            int isSaved = await _context.SaveChangesAsync();

            if (isSaved <= 0)
            {
                _logger.LogError("Failed to add proposal statistics for user with email {Email}.", email);
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateTotalProposalsAsync(bool proposial, string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("User with email {Email} not found.", email);
                    return false;
                }

                var userProposialStats = _context.ProposalStatistics
                    .FirstOrDefault(ps => ps.UserId == user.Id);

                if (userProposialStats == null)
                {
                    _logger.LogWarning("No proposal statistics found for user with email {Email}.", email);
                    return false;
                }

                int newTotalProposals = (userProposialStats.TotalProposals ?? 0) + 1;
                userProposialStats.TotalProposals = newTotalProposals;

                if (proposial)
                {
                    int newApprovedProposals = (userProposialStats.ApprovedProposals ?? 0) + 1;
                    userProposialStats.ApprovedProposals = newApprovedProposals;
                }
                else
                {
                    int newRejectedProposals = (userProposialStats.RejectedProposals ?? 0) + 1;
                    userProposialStats.RejectedProposals = newRejectedProposals;
                }

                int newAcceptedRate = userProposialStats.TotalProposals > 0
                    ? (int)(((double)userProposialStats.ApprovedProposals / userProposialStats.TotalProposals) * 100)
                    : 0;

                userProposialStats.AcceptedRate = newAcceptedRate;

                userProposialStats.UpdatedAt = DateTime.UtcNow;

                int isSaved = await _context.SaveChangesAsync();

                if (isSaved <= 0)
                {
                    _logger.LogError("Failed to update proposal statistics for user with email {Email}.", email);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while updating proposal statistics for user with email {Email}.", email);
                return false;
            }
        }
    }
}
