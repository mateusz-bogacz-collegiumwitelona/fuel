using Data.Context;
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
    public class ProposalStatisticServices
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProposalStatisticServices(
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
                TotalProposals = proposals.TotalProposals,
                ApprovedProposals = proposals.ApprovedProposals,
                RejectedProposals = proposals.RejectedProposals,
                AcceptedRate = (int)proposals.AcceptedRate,
                UpdatedAt = proposals.UpdatedAt
            };
        }


        //public async Task UpdateTotalProposalsAsync(bool proposial, string email)
        //{
        //    var user = await _userManager.FindByEmailAsync(email);

        //    if (user == null) return null;

        //    var proposals = _context.ProposalStatisicts
        //        .FirstOrDefault(ps => ps.UserId == user.Id);

        //    if (proposial)
        //    {
        //        int approved = proposals.ApprovedProposals++;

        //    }
        //}
    }
}
