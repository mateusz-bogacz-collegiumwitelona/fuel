using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace contlollers.Controllers.Test
{
    [ApiController]
    [Route("api/test")]
    [EnableCors("AllowClient")]
    public class TestContloller : ControllerBase
    {
        private readonly ITestServices _test;

        public TestContloller(
            ITestServices test
            )
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

    }
}
