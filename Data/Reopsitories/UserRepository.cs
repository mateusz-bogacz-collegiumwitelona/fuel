using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Linq.Expressions;

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

        public async Task<List<GetUserListResponse>> GetUserListAsync(TableRequest request)
        {
            var rolePriority = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
            {
                { "admin", 2 },
                { "user", 1 }
            };

            var query = from u in _context.Users
                        where !u.IsDeleted
                        join ur in _context.UserRoles on u.Id equals ur.UserId
                        join r in _context.Roles on ur.RoleId equals r.Id
                        select new
                        {
                            u.UserName,
                            u.Email,
                            Role = r.Name,
                            u.CreatedAt
                        };

            if (!string.IsNullOrEmpty(request.Search))
            {
                string searchLower = request.Search.ToLower();
                query = query.Where(u =>
                    u.UserName.ToLower().Contains(searchLower) ||
                    u.Email.ToLower().Contains(searchLower) ||
                    u.Role.ToLower().Contains(searchLower)
                );
            }

            var list = await query.AsNoTracking().ToListAsync();

            if (!string.IsNullOrEmpty(request.SortBy))
            {
                switch (request.SortBy.ToLower())
                {
                    case "username":
                        list = request.SortDirection?.ToLower() == "desc"
                            ? list.OrderByDescending(u => u.UserName).ToList()
                            : list.OrderBy(u => u.UserName).ToList();
                        break;

                    case "email":
                        list = request.SortDirection?.ToLower() == "desc"
                            ? list.OrderByDescending(u => u.Email).ToList()
                            : list.OrderBy(u => u.Email).ToList();
                        break;

                    case "roles":
                        list = request.SortDirection?.ToLower() == "desc"
                            ? list.OrderByDescending(u => rolePriority.ContainsKey(u.Role.ToLower()) ? rolePriority[u.Role.ToLower()] : 0).ToList()
                            : list.OrderBy(u => rolePriority.ContainsKey(u.Role.ToLower()) ? rolePriority[u.Role.ToLower()] : 0).ToList();
                        break;

                    case "createdat":
                        list = request.SortDirection?.ToLower() == "desc"
                            ? list.OrderByDescending(u => u.CreatedAt).ToList()
                            : list.OrderBy(u => u.CreatedAt).ToList();
                        break;

                    default:
                        list = list.OrderByDescending(u => rolePriority.ContainsKey(u.Role.ToLower()) ? rolePriority[u.Role.ToLower()] : 0).ToList();
                        break;
                }
            }

            return list.Select(u => new GetUserListResponse
            {
                UserName = u.UserName,
                Email = u.Email,
                Roles = u.Role,
                CreatedAt = u.CreatedAt
            }).ToList();
        }

        public async Task<bool> AddBanRecordAsync(ApplicationUser user, ApplicationUser admin, SetLockoutForUserRequest request )
        {
            var ban = new BanRecord
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                User = user,
                Reason = request.Reason,
                BannedAt = DateTime.UtcNow,
                BannedUntil = request.Days.HasValue ? DateTime.UtcNow.AddDays(request.Days.Value) : null,
                IsActive = true,
                AdminId = admin.Id,
                Admin = admin
            };

            await _context.BanRecords.AddAsync(ban);

            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task DeactivateActiveBansAsync(Guid userId, Guid unbannedByAdminId)
        {
            var activeBans = await _context.BanRecords
                .Where(b => b.UserId == userId && b.IsActive)
                .ToListAsync();

            if (!activeBans.Any())
                return; 

            foreach (var ban in activeBans)
            {
                ban.IsActive = false;
                ban.UnbannedAt = DateTime.UtcNow;
                ban.UnbannedByAdminId = unbannedByAdminId;
            }

            await _context.SaveChangesAsync();
        }

        public async Task<List<BanRecord>> GetExpiredBans(CancellationToken cancellation)
            => await _context.BanRecords
                .Where(b => b.IsActive && b.BannedUntil.HasValue && b.BannedUntil <= DateTime.UtcNow)
                .ToListAsync(cancellation);
    }
}