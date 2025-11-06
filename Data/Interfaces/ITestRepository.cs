using DTO.Responses;

namespace Data.Interfaces
{
    public interface ITestRepository
    {
        Task<TestRedisResponse> GetIsRedisConnectAsync();
        Task<TestPostgresResponse> GetIsPostgresConnectAsync();
        Task<TestMinioResponse> GetIsMinioConnectAsync();
    }
}
