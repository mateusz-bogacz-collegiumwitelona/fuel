using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface IUserServices
    {
        Task<Result<bool>> ChangeUserNameAsync(string email, string userName);
        Task<Result<GetUserInfoResponse>> GetUserInfoAsync(string email);
        Task<Result<IdentityResult>> ChangeUserEmailAsync(string oldEmail, string newEmail);
    }
}
