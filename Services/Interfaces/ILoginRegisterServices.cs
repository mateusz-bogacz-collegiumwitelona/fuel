using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface ILoginRegisterServices
    {
        Task<Result<LoginResponse>> HandleLoginAsync(DTO.Requests.LoginRequest request);
        Task<Result<ConfirmEmailRequest>> RegisterNewUser(RegisterNewUserRequest request);
        Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request);
    }
}
