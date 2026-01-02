using Data.Context;
using Data.Helpers;
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
        private UserFilterSorting _filters = new();

        public UserRepository(
            ApplicationDbContext context, 
            ILogger<UserRepository> logger,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _logger = logger;
        }

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
                            UserId = u.Id,
                            UserName = u.UserName,
                            Email = u.Email,
                            Role = r.Name,
                            CreatedAt = u.CreatedAt
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

            var users = await query.AsNoTracking().ToListAsync();

            var userIds = users.Select(u => u.UserId).ToList();
            var bannedUserIds = await _context.BanRecords
                .Where(b => userIds.Contains(b.UserId) && b.IsActive)
                .Select(b => b.UserId)
                .Distinct()
                .ToListAsync();

            var bannedUsersSet = new HashSet<Guid>(bannedUserIds);

            var list = users.Select(u => new GetUserListResponse
            {
                UserName = u.UserName,
                Email = u.Email,
                Roles = u.Role,
                CreatedAt = u.CreatedAt,
                IsBanned = bannedUsersSet.Contains(u.UserId)
            }).ToList();

            list = _filters.ApplySorting(list, request.SortBy, request.SortDirection, rolePriority);

            return list;
        }

        
        

        public async Task<bool> ReportUserAsync(
            ApplicationUser reported, 
            ApplicationUser reportedBy, 
            string reason)
        {
            var report = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportedUserId = reported.Id,
                ReportedUser = reported,
                ReportingUserId = reportedBy.Id,
                ReportingUser = reportedBy,
                Description = reason,
                CreatedAt = DateTime.UtcNow,
                Status = Data.Enums.ReportStatusEnum.Pending
            };

            await _context.ReportUserRecords.AddAsync(report);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }
    }
}