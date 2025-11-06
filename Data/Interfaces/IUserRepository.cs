using DTO.Responses;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<GetUserInfoResponse> GetUserInfoAsync(string email);
    }
}
