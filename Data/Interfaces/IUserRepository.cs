using DTO.Responses;

namespace Data.Interfaces
{
    public interface IUserRepository
    {
        Task<GetUserInfoResponse> GetUserInfoAsync(string email);
        Task<bool> ChangeUserNameAsync(string email, string userName);
    }
}
