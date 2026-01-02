using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface ILoginRegisterServices
    {
        Task<Result<LoginResponse>> HandleLoginAsync(LoginRequest request);
        Task<Result<IdentityResult>> LogoutAsync();
        Task<Result<LoginResponse>> HandleRefreshAsync();
        Task<Result<LoginResponse>> GetCurrentUserAsync(Guid userId);
        Task<Result<IdentityResult>> RegisterNewUserAsync(RegisterNewUserRequest request);
        Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request);
        Task<Result<IdentityResult>> ForgotPasswordAsync(string email);
        Task<Result<IdentityResult>> SetNewPassowrdAsync(ResetPasswordRequest request);
        Task<Result<LoginResponse>> RegisterWithFacebookTokenAsync(string accessToken, HttpContext httpContext);
        Task<Result<LoginResponse>> LoginWithFacebookTokenAsync(string token, HttpContext httpContext);    }
}
