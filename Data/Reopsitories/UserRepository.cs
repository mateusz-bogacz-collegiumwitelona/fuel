using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

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

        public async Task<bool> IsUserDeleted(ApplicationUser user)
         => await _context.Users
                .Where(u => u.Id == user.Id && u.IsDeleted)
                .AnyAsync();

        public async Task<IdentityResult> DeleteUserAsync(ApplicationUser user)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == user.Id);

                if (existingUser == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found for deletion.", user.Id);
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "UserNotFound",
                        Description = "User not found."
                    });
                }

                existingUser.IsDeleted = true;
                existingUser.DeletdAt = DateTime.UtcNow;
                _context.Users.Update(existingUser);
                var result = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                if (result > 0)
                {
                    return IdentityResult.Success;
                }
                else
                {
                    return IdentityResult.Failed(new IdentityError
                    {
                        Code = "DeletionFailed",
                        Description = "User deletion failed."
                    });
                    await transaction.RollbackAsync();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user with ID {UserId}.", user.Id);
                await transaction.RollbackAsync();
                return IdentityResult.Failed(new IdentityError
                {
                    Code = "Exception",
                    Description = "An error occurred during user deletion."
                });
            }
        }
    }
}