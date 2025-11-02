using Services.Helpers;

namespace Services.Interfaces
{
    public interface IUserServices
    {
        Task<Result<bool>> ChangeUserNameAsync(string email, string userName);
    }
}
