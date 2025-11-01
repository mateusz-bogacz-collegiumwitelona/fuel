using DTO.Requests;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<string> RegisterNewUserAsync(RegisterNewUserRequest request);
        Task<IdentityResult> ConfirmEmailAsync(ConfirmEmailRequest request);
    }
}
