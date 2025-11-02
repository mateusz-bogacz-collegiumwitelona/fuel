using DTO.Requests;
using Microsoft.AspNetCore.Identity;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<bool> ChangeUserNameAsync(string email, string userName);
    }
}
