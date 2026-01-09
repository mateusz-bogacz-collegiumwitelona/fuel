using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> IsUserDeleted(ApplicationUser user);
        Task<IdentityResult> DeleteUserAsync(ApplicationUser user);
        Task<List<GetUserListResponse>> GetUserListAsync(TableRequest request);
    }
}
