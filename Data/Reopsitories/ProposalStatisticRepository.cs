using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Reopsitories
{
    public class ProposalStatisticRepository : IProposalStatisticRepository
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProposalStatisticRepository(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager
            )
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<GetProposalStatisticResponse> GetUserProposalStatisticAsync(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

            if (user == null) return null;

            var proposals = _context.ProposalStatisicts
                .FirstOrDefault(ps => ps.UserId == user.Id);

            if (proposals == null) return null;

            return new GetProposalStatisticResponse
            {
                TotalProposals = proposals.TotalProposals ?? 0,
                ApprovedProposals = proposals.ApprovedProposals ?? 0,
                RejectedProposals = proposals.RejectedProposals ?? 0,
                AcceptedRate = proposals.AcceptedRate ?? 0,
                UpdatedAt = proposals.UpdatedAt
            };
        }

        public async Task<bool> AddProposalStatisticRecordAsunc(string email)
        {
            var user = await _userManager.FindByEmailAsync(email);

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

            await _context.ProposalStatisicts.AddAsync(proposal);
            int isSaved = await _context.SaveChangesAsync();

            if (isSaved <= 0) return false;
            return true;
        }

        public async Task<bool> UpdateTotalProposalsAsync(bool proposial, string email)
        {
            return true;
        }
    }
}
