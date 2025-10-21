using DTO.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Data.Interfaces
{
    public interface ITestRepository
    {
        Task<TestRedisResponse> GetIsRedisConnectAsync();
        Task<TestPostgresResponse> GetIsPostgresConnectAsync();
    }
}
