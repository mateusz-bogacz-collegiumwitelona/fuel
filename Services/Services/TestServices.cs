using Data.Interfaces;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class TestServices : ITestServices
    {
        private readonly ITestRepository _test;
        private readonly ILogger<TestServices> _logger;
        private readonly GetStatusCodeHelper _statusCodeHelper = new GetStatusCodeHelper();

        public TestServices(
            ITestRepository test,
            ILogger<TestServices> logger
            )
        {
            _test = test;
            _logger = logger;
        }

        public async Task<Result<TestRedisResponse>> GetIsRedisConnectAsync()
        {
            try
            {
                var result = await _test.GetIsRedisConnectAsync();

                if (result.Status != 200)
                {
                    var logErrors = result.Messages ?? new List<string> { "Unknown Redis Error" };
                    _logger.LogWarning($"Redis test failed: {string.Join("; ", logErrors)}");

                    var statusCode = GetStatusCodeHelper.MapStatusCode(result.Status);

                    return Result<TestRedisResponse>.Bad(
                    "Redis connection failed",
                    statusCode,
                    logErrors,
                    result
                    );
                }

                return Result<TestRedisResponse>.Good(
                    "Redis is reachable",
                    StatusCodes.Status200OK,
                    result
                    );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving stations: {ex.Message} | {ex.InnerException}");
                return Result<TestRedisResponse>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }

        public async Task<Result<TestPostgresResponse>> GetIsPostgresConnectAsync()
        {
            try
            {
                var result = await _test.GetIsPostgresConnectAsync();

                if (result.Status != 200)
                {
                    var logErrors = result.Message != null ? new List<string> { result.Message } : new List<string> { "Unknown Postgres Error" };
                    _logger.LogWarning($"Postgres test failed: {string.Join("; ", logErrors)}");

                    var statusCode = GetStatusCodeHelper.MapStatusCode(result.Status);

                    return Result<TestPostgresResponse>.Bad(
                        "Postgres connection failed",
                        statusCode,
                        logErrors,
                        result
                    );
                }

                return Result<TestPostgresResponse>.Good(
                    "Postgres is reachable",
                    StatusCodes.Status200OK,
                    result
                );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving stations: {ex.Message} | {ex.InnerException}");
                return Result<TestPostgresResponse>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    }
}
