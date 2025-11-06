using Data.Context;
using Data.Interfaces;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Minio.DataModel.Args;
using Minio.Exceptions;
using StackExchange.Redis;
using System.Diagnostics;

namespace Data.Reopsitories
{
    public class TestRepository : ITestRepository
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<TestRepository> _logger;
        private readonly ApplicationDbContext _context;
        private readonly IMinioClient _minioClient;
        private readonly IConfiguration _config;
        public TestRepository(
            IConnectionMultiplexer redis,
            ILogger<TestRepository> logger,
            ApplicationDbContext context,
            IMinioClient minioClient,
            IConfiguration config
            )
        {
            _redis = redis;
            _logger = logger;
            _context = context;
            _minioClient = minioClient;
            _config = config;
        }

        public async Task<TestRedisResponse> GetIsRedisConnectAsync()
        {
            var response = new TestRedisResponse();
            var stopWatch = new Stopwatch();

            stopWatch.Start();

            try
            {
                var redisDb = _redis.GetDatabase();

                if (redisDb == null)
                {
                    _logger.LogError("Redis database instance is null");
                    response.Status = 500;
                    response.Messages = new List<string> { "Redis database instance is null" };
                    return response;
                }

                var endpoints = _redis.GetEndPoints();

                if (endpoints == null || endpoints.Length == 0)
                {
                    _logger.LogWarning("No Redis endpoints found");
                    response.Status = 404;
                    response.Messages = new List<string> { "No Redis endpoints found" };
                    return response;
                }

                var time = await redisDb.PingAsync();
                stopWatch.Stop();


                response.Status = 200;
                response.Messages = new List<string> { "Redis connected successfully" };
                response.ResponseTime = $"{time.TotalMilliseconds}ms";
                response.Endpoints = endpoints.Select(e => e.ToString()).ToList();
                _logger.LogInformation($"Redis connected in {time.TotalMilliseconds}ms");
            }
            catch (RedisConnectionException rcex)
            {
                stopWatch.Stop();
                _logger.LogError(rcex, "Redis connection error");
                response.Status = 503;
                response.Messages = new List<string> { "Redis connection failed", rcex.Message };
            }
            catch (RedisTimeoutException rtex)
            {
                stopWatch.Stop();
                _logger.LogError(rtex, "Redis timeout");
                response.Status = 504;
                response.Messages = new List<string> { "Redis connection timeout", rtex.Message };
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                _logger.LogError(ex, "Unexpected error while testing Redis");
                response.Status = 500;
                response.Messages = new List<string> { "Unexpected error", ex.Message };
            }

            response.ResponseTime ??= $"{stopWatch.ElapsedMilliseconds}ms";
            return response;
        }

        public async Task<TestPostgresResponse> GetIsPostgresConnectAsync()
        {
            var response = new TestPostgresResponse();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                response.CanConnect = canConnect;

                if (!canConnect)
                {
                    _logger.LogError("Cannot connect to the database");
                    stopWatch.Stop();
                    response.Status = 503;
                    response.Message = "Cannot connect to the database";
                    response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
                    return response;
                }

                await using var conn = _context.Database.GetDbConnection();

                await conn.OpenAsync();

                bool hasPostgis = false;
                string? postgisVersion = null;

                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "SELECT postgis_version();";
                    try
                    {
                        var version = await cmd.ExecuteScalarAsync();

                        if (version != null)
                        {
                            hasPostgis = true;
                            postgisVersion = version.ToString();
                        }
                    }
                    catch
                    {

                        hasPostgis = false;
                    }
                }

                stopWatch.Stop();

                response.Status = 200;
                response.Message = hasPostgis
                    ? "PostgreSQL and PostGIS are active"
                    : "PostgreSQL connected, but PostGIS extension is missing";

                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
                response.PostgisInstalled = hasPostgis;
                response.PostgisVersion = postgisVersion;

                if (hasPostgis)
                {
                    _logger.LogInformation("PostgreSQL connected and PostGIS extension is active. Response time: {Time}ms", stopWatch.ElapsedMilliseconds);
                }
                else
                {
                    _logger.LogWarning("PostgreSQL connected, but PostGIS extension is not installed. Response time: {Time}ms", stopWatch.ElapsedMilliseconds);
                }


                return response;
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                _logger.LogError(ex, "Error while testing PostgreSQL/PostGIS connection");
                response.Status = 500;
                response.Message = $"Error: {ex.Message}";
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
                response.CanConnect = false;
                response.PostgisInstalled = false;
                return response;
            }
        }

        public async Task<TestMinioResponse> GetIsMinioConnectAsync()
        {
            var response = new TestMinioResponse();
            var stopWatch = new Stopwatch();
            stopWatch.Start();

            try
            {
                var endpoint = _config["MinIO:Endpoint"];
                var bucketName = _config["MinIO:BucketName"];

                response.Endpoint = endpoint;
                response.BucketName = bucketName;

                var buckets = await _minioClient.ListBucketsAsync();

                response.CanConnect = true;
                response.AvalableBuckets = buckets?.Buckets?
                    .Select(b => b.Name)
                    .ToList() ?? new List<string>();

                if (!string.IsNullOrEmpty(bucketName))
                {
                    var bucketExistsArgs = new BucketExistsArgs()
                        .WithBucket(bucketName);

                    response.IsBucketExist = await _minioClient.BucketExistsAsync(bucketExistsArgs);

                    if (!response.IsBucketExist)
                        _logger.LogWarning("Bucket '{BucketName}' does not exist in MinIO", bucketName);
                }

                stopWatch.Stop();

                response.Status = 200;
                response.Message = response.IsBucketExist
                    ? $"MinIO connected successfully. Bucket '{bucketName}' exists."
                    : $"MinIO connected successfully. Bucket '{bucketName}' does not exist.";
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
                _logger.LogInformation(
                    "MinIO connected successfully. Response time: {Time}ms",
                    stopWatch.ElapsedMilliseconds,
                    response.AvalableBuckets?.Count ?? 0);
            }
            catch (MinioException mex) when (mex.Message.Contains("Connection refused"))
            {
                stopWatch.Stop();
                _logger.LogError(mex, "MinIO connection refused");
                response.Status = 503;
                response.Message = "MinIO connection refused";
                response.CanConnect = false;
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
            }
            catch (MinioException mex) when (mex.Message.Contains("Access Denied"))
            {
                stopWatch.Stop();
                _logger.LogError(mex, "MinIO auth failed");
                response.Status = 401;
                response.Message = "MinIO auth failed - check credentials";
                response.CanConnect = false;
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
            }
            catch (BucketNotFoundException bnfex)
            {
                stopWatch.Stop();
                _logger.LogError(bnfex, "MinIO bucket not found");
                response.Status = 404;
                response.Message = "MinIO bucket not found";
                response.CanConnect = true;
                response.IsBucketExist = false;
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
            }
            catch (InvalidEndpointException ieex)
            {
                stopWatch.Stop();
                _logger.LogError(ieex, "MinIO invalid endpoint");
                response.Status = 500;
                response.Message = "MinIO invalid endpoint";
                response.CanConnect = false;
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
            }
            catch (Exception ex)
            {
                stopWatch.Stop();
                _logger.LogError(ex, "Unexpected error while testing MinIO connection");
                response.Status = 500;
                response.Message = $"Unexpected error: {ex.Message}";
                response.CanConnect = false;
                response.ResponseTime = $"{stopWatch.ElapsedMilliseconds}ms";
            }

            return response;
        }
    }
}
