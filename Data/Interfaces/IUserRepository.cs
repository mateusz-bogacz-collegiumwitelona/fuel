using DTO.Requests;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<IdentityResult> RegisterNewUser(RegisterNewUserRequest request);
        Task<string> GenerateConfirEmailTokenAsync(string email);
        Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailRequest request);
        Task<string> GeneratePasswordResetToken(string email);
    }
}
