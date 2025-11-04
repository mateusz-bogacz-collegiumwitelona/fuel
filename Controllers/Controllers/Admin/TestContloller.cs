using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace Controllers.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/test")]
    [EnableCors("AllowClient")]
    [Authorize(Roles = "Admin")]
    public class TestContloller : ControllerBase
    {
        private readonly ITestServices _test;

        public TestContloller(ITestServices test)
        {
            _test = test;
        }

        /// <summary>
        /// Test the connection to the Redis server.
        /// </summary>
        /// <remarks>
        /// Description: Checks if the application can successfully connect to the configured Redis instance.
        /// Returns a JSON object describing the connection result, response time, and available endpoints.
        ///
        /// Example response — Successful connection
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "Redis connection successful.",
        ///   "errors": null,
        ///   "responseTime": "42ms",
        ///   "endpoints": [ "localhost:6379" ]
        /// }
        /// ```
        ///
        /// Example response — Failed connection
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Redis connection failed.",
        ///   "errors": [ "Timeout while connecting to Redis.", "Connection refused." ],
        ///   "responseTime": "5000ms",
        ///   "endpoints": null
        /// }
        /// ```
        ///
        /// Notes
        /// - `success` — indicates if the Redis connection test passed.
        /// - `responseTime` — total time measured for the connection attempt.
        /// - `endpoints` — list of Redis endpoints discovered (if available).
        /// </remarks>
        /// <response code="200">Redis connected successfully</response>
        /// <response code="503">Cannot connect to Redis or connection timeout</response>
        /// <response code="500">Unexpected internal server error</response>

        [HttpGet("redis")]
        public async Task<IActionResult> GetGetIsRedisConnectAsync()
        {
            var result = await _test.GetIsRedisConnectAsync();

            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors,
                    Data = result.Data
                });
        }


        /// <summary>
        /// Test the connection to PostgreSQL and verify if PostGIS is active.
        /// </summary>
        /// <remarks>
        /// Description: Checks if the application can connect to the PostgreSQL database and if PostGIS is installed and active.
        /// Returns a JSON object describing connection status, PostGIS availability, and version.
        ///
        /// Example response — Good connection (PostGIS active)
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "PostgreSQL connected and PostGIS active.",
        ///   "errors": null,
        ///   "responseTime": "38ms",
        ///   "canConnect": true,
        ///   "postgisInstalled": true,
        ///   "postgisVersion": "3.3.3"
        /// }
        /// ```
        ///
        /// Example response — Bad connection or PostGIS missing
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "Cannot connect to PostgreSQL or PostGIS not installed.",
        ///   "errors": [ "Timeout while connecting to database.", "PostGIS extension missing." ],
        ///   "responseTime": "5000ms",
        ///   "canConnect": false,
        ///   "postgisInstalled": false,
        ///   "postgisVersion": null
        /// }
        /// ```
        ///
        /// Notes
        /// - `success` indicates whether the database connection and PostGIS check passed.
        /// - `canConnect` shows if the database connection itself succeeded.
        /// - `postgisInstalled` confirms if the PostGIS extension is installed.
        /// - `postgisVersion` contains the installed PostGIS version (if available).
        /// - `responseTime` measures how long the connection test took.
        /// </remarks>
        /// <response code="200">PostgreSQL connected and PostGIS active</response>
        /// <response code="503">Cannot connect to database</response>
        /// <response code="500">Unexpected internal server error</response>

        [HttpGet("postgres")]
        public async Task<IActionResult> GetIsPostgresConnectAsync()
        {
            var result = await _test.GetIsPostgresConnectAsync();
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors,
                    Data = result.Data
                });
        }

        /// <summary>
        /// Test the connection to the MinIO storage server.
        /// </summary>
        /// <remarks>
        /// Description: Checks if the application can successfully connect to the configured MinIO instance.
        /// Returns a JSON object describing the connection result, response time, available buckets, and bucket existence status.
        ///
        /// Example response — Successful connection with existing bucket
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "MinIO connected successfully. Bucket 'fuel-prices' exists.",
        ///   "errors": null,
        ///   "data": {
        ///     "status": 200,
        ///     "message": "MinIO connected successfully. Bucket 'fuel-prices' exists.",
        ///     "responseTime": "45ms",
        ///     "canConnect": true,
        ///     "endpoint": "minio:9000",
        ///     "isBucketExist": true,
        ///     "bucketName": "fuel-prices",
        ///     "avalableBuckets": ["fuel-prices", "avatars"]
        ///   }
        /// }
        /// ```
        ///
        /// Example response — Connection successful but bucket missing
        /// ```json
        /// {
        ///   "success": true,
        ///   "message": "MinIO connected successfully. Bucket 'fuel-prices' does not exist.",
        ///   "errors": null,
        ///   "data": {
        ///     "status": 200,
        ///     "message": "MinIO connected successfully. Bucket 'fuel-prices' does not exist.",
        ///     "responseTime": "52ms",
        ///     "canConnect": true,
        ///     "endpoint": "minio:9000",
        ///     "isBucketExist": false,
        ///     "bucketName": "fuel-prices",
        ///     "avalableBuckets": []
        ///   }
        /// }
        /// ```
        ///
        /// Example response — Failed connection
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "MinIO connection failed",
        ///   "errors": ["MinIO connection refused"],
        ///   "data": {
        ///     "status": 503,
        ///     "message": "MinIO connection refused",
        ///     "responseTime": "2034ms",
        ///     "canConnect": false,
        ///     "endpoint": "minio:9000",
        ///     "isBucketExist": false,
        ///     "bucketName": "fuel-prices",
        ///     "avalableBuckets": null
        ///   }
        /// }
        /// ```
        ///
        /// Example response — Authentication failed
        /// ```json
        /// {
        ///   "success": false,
        ///   "message": "MinIO connection failed",
        ///   "errors": ["MinIO auth failed - check credentials"],
        ///   "data": {
        ///     "status": 401,
        ///     "message": "MinIO auth failed - check credentials",
        ///     "responseTime": "156ms",
        ///     "canConnect": false,
        ///     "endpoint": "minio:9000",
        ///     "isBucketExist": false,
        ///     "bucketName": null,
        ///     "avalableBuckets": null
        ///   }
        /// }
        /// ```
        ///
        /// Notes
        /// - `canConnect` — indicates if the MinIO server is reachable.
        /// - `isBucketExist` — indicates if the configured bucket exists on the server.
        /// - `responseTime` — total time measured for the connection attempt.
        /// - `endpoint` — configured MinIO endpoint.
        /// - `avalableBuckets` — list of all buckets discovered on the MinIO server.
        /// - `bucketName` — name of the configured primary bucket.
        /// </remarks>
        /// <response code="200">MinIO connected successfully (bucket may or may not exist)</response>
        /// <response code="401">Authentication failed - invalid credentials</response>
        /// <response code="404">Bucket not found</response>
        /// <response code="500">Invalid endpoint configuration or unexpected error</response>
        /// <response code="503">Cannot connect to MinIO server</response>
        [HttpGet("minio")]
        public async Task<IActionResult> GetIsMinioConnectAsync()
        {
            var result = await _test.GetIsMinioConnectAsync();
            return result.IsSuccess
                ? StatusCode(result.StatusCode, result.Data)
                : StatusCode(result.StatusCode, new
                {
                    success = false,
                    message = result.Message,
                    errors = result.Errors,
                    Data = result.Data
                });
        }

    }
}
