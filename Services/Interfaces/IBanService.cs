using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IBanService
    {
        Task<Result<IdentityResult>> LockoutUserAsync(string adminEmail, SetLockoutForUserRequest request);
        Task<Result<IdentityResult>> UnlockUserAsync(string adminEmail, string userEmail);
        Task<Result<ReviewUserBanResponses>> GetUserBanInfoAsync(string email);
    }
}
