using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography.X509Certificates;

namespace Data.Reopsitories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserRepository> _logger;

        public UserRepository(
            ApplicationDbContext context, 
            ILogger<UserRepository> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<GetUserInfoResponse> GetUserInfoAsync(string email)
            => await _context.Users
            .Where(u => u.Email == email)
            .Select(u => new GetUserInfoResponse
            {
                UserName = u.UserName,
                Email = u.Email,
                CreatedAt = u.CreatedAt,
                ProposalStatistics = new GetProposalStatisticResponse
                {
                    TotalProposals = (int)u.ProposalStatistic.TotalProposals,
                    ApprovedProposals = (int)u.ProposalStatistic.ApprovedProposals,
                    RejectedProposals = (int)u.ProposalStatistic.RejectedProposals,
                    AcceptedRate = (int)u.ProposalStatistic.AcceptedRate,
                    UpdatedAt = u.ProposalStatistic.UpdatedAt
                }
            })
            .FirstOrDefaultAsync();
        
    }
}