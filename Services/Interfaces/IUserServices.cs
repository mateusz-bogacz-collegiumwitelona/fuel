using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IUserServices
    {
        Task<Result<IdentityResult>> ChangeUserNameAsync(string email, string userName);
        Task<Result<GetUserInfoResponse>> GetUserInfoAsync(string email);
        Task<Result<IdentityResult>> ChangeUserEmailAsync(string oldEmail, string newEmail);
        Task<Result<IdentityResult>> ChangeUserPasswordAsync(string email, ChangePasswordRequest request);
        Task<Result<IdentityResult>> DeleteUserAsyc(string email, DeleteAccountRequest request);
        Task<Result<PagedResult<GetUserListResponse>>> GetUserListAsync(GetPaggedRequest pagged, TableRequest request);
        Task<Result<IdentityResult>> ChangeUserRoleAsync(string email, string newRole);
        Task<Result<IdentityResult>> LockoutUserAsync(string adminEmail, SetLockoutForUserRequest request);    }
}
