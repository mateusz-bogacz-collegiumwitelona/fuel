using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface ILoginRegisterServices
    {
        Task<Result<IdentityResult>> HandleLoginAsync(DTO.Requests.LoginRequest request);
        Task<Result<IdentityResult>> LogoutAsync();
        Task<Result<IdentityResult>> RegisterNewUserAsync(RegisterNewUserRequest request);
        Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request);
        Task<Result<IdentityResult>> ForgotPasswordAsync(string email);
        Task<Result<IdentityResult>> SetNewPassowrdAsync(ResetPasswordRequest request);
        Task<Result<IdentityResult>> HandleRefreshAsync();
    }
}
