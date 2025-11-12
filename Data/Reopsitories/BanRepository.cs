using Data.Context;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;

namespace Data.Reopsitories
{
    public class BanRepository : IBanRepository
    {
        private readonly ApplicationDbContext _context;
        
        public BanRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<bool> AddBanRecordAsync(ApplicationUser user, ApplicationUser admin, SetLockoutForUserRequest request)
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

        public async Task<ReviewUserBanResponses> GetUserBanInfoAsync(string email)
            => await _context.BanRecords
                .Where(b => b.User.Email == email && b.IsActive)
                .Select(b => new ReviewUserBanResponses
                {
                    UserName = b.User.UserName,
                    Reason = b.Reason,
                    BannedAt = b.BannedAt,
                    BannedUntil = b.BannedUntil ?? DateTime.MaxValue,
                    BannedBy = b.Admin.UserName
                })
                .FirstOrDefaultAsync();
    }
}
