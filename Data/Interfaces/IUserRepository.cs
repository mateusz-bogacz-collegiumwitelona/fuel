using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<GetUserInfoResponse> GetUserInfoAsync(string email);
        Task<bool> IsUserDeleted(ApplicationUser user);
        Task<IdentityResult> DeleteUserAsync(ApplicationUser user);
        Task<List<GetUserListResponse>> GetUserListAsync(TableRequest request);
        Task<bool> AddBanRecordAsync(ApplicationUser user, ApplicationUser admin, SetLockoutForUserRequest request);
        Task DeactivateActiveBansAsync(Guid userId, Guid unbannedByAdminId);
        Task<List<BanRecord>> GetExpiredBans(CancellationToken cancellation);
    }
}
