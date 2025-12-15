using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data.Reopsitories
{
    public class ProposalStatisticRepository : IProposalStatisticRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProposalStatisticRepository> _logger;

        public ProposalStatisticRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            ILogger<ProposalStatisticRepository> logger
            )
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(ApplicationUser user)
        {

            var proposals = _context.ProposalStatistics
                .FirstOrDefault(ps => ps.UserId == user.Id);

            if (proposals == null)
            {
                _logger.LogWarning("No proposal statistics found for user with email {Email}.", user.Email);
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

        public async Task<bool> AddProposalStatisticRecordAsync(ApplicationUser user)
        {

            var proposal = new ProposalStatistic
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
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
                _logger.LogError("Failed to add proposal statistics for user with email {Email}.", user.Email);
                return false;
            }

            return true;
        }

        public async Task<bool> UpdateTotalProposalsAsync(bool isAccepted, Guid userId)
        {
            var userProposalStats = await _context.ProposalStatistics
                .FirstOrDefaultAsync(ps => ps.UserId == userId);

            if (userProposalStats == null)
            {
                _logger.LogWarning("No proposal statistics found for user {UserId}", userId);
                return false;
            }

            userProposalStats.TotalProposals = (userProposalStats.TotalProposals ?? 0) + 1;

            if (isAccepted)
            {
                userProposalStats.ApprovedProposals = (userProposalStats.ApprovedProposals ?? 0) + 1;
                userProposalStats.Points = (userProposalStats.Points ?? 0) + 1;
            }
            else
            {
                userProposalStats.RejectedProposals = (userProposalStats.RejectedProposals ?? 0) + 1;
            }

            userProposalStats.AcceptedRate = userProposalStats.TotalProposals > 0
                ? (int)(((double)(userProposalStats.ApprovedProposals ?? 0) / userProposalStats.TotalProposals.Value) * 100)
                : 0;

            userProposalStats.UpdatedAt = DateTime.UtcNow;


            int savedCount = await _context.SaveChangesAsync();

            if (savedCount <= 0)
            {
                _logger.LogError("Failed to update proposal statistics for user {UserId}", userId);
                return false;
            }

            _logger.LogInformation(
                "Updated statistics for user {UserId}: Total={Total}, Approved={Approved}, Rate={Rate}%",
                userId, userProposalStats.TotalProposals, userProposalStats.ApprovedProposals, userProposalStats.AcceptedRate);

            return true;
        }
        public async Task<List<TopUserResponse>> GetTopUserListAsync()
         => await _context.ProposalStatistics
                .OrderByDescending(ps => ps.Points)
                .Take(10)
                .Select(ps => new TopUserResponse
                {
                    UserName = ps.User.UserName,
                    TotalProposals = ps.TotalProposals,
                    ApprovedProposals = ps.ApprovedProposals,
                    RejectedProposals = ps.RejectedProposals,
                    AcceptedRate = ps.AcceptedRate,
                    Points = ps.Points
                })
                .ToListAsync();
    }
}
