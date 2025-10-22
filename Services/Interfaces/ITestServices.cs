using DTO.Responses;
using Services.Helpers;

namespace Services.Interfaces
{
    public interface ITestServices
    {
        Task<Result<TestRedisResponse>> GetIsRedisConnectAsync();
        Task<Result<TestPostgresResponse>> GetIsPostgresConnectAsync();
    }
}
