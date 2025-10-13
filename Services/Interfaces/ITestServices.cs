using DTO.Responses;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Services.Interfaces
{
    public interface ITestServices
    {
        Task<Result<TestRedisResponse>> GetIsRedisConnectAsync();
        Task<Result<TestPostgresResponse>> GetIsPostgresConnectAsync();
    }
}
