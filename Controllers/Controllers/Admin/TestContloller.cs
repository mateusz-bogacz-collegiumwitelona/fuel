using Data.Context;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NetTopologySuite.Geometries;
using Services.Interfaces;
using StackExchange.Redis;
using System.Net.WebSockets;

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
        /// Tests the connection to Redis.
        /// </summary>
        /// <remarks>
        /// <![CDATA[
        /// Returns a JSON with connection status:
        ///
        /// GOOD (Redis connected successfully):
        /// {
        ///   "success": bool,
        ///   "message": string,
        ///   "errors": List<string> or null,
        ///   "responseTime": string,
        ///   "endpoints": List<string> or null
        /// }
        ///
        /// BAD (Redis connection failed):
        /// {
        ///   "success": bool,
        ///   "message": string,
        ///   "errors": List<string>,
        ///   "responseTime": string,
        ///   "endpoints": List<string> or null
        /// }
        /// ]]>
        /// </remarks>
        /// <response code="200">Redis connected successfully</response>
        /// <response code="503">Cannot connect to Redis or timeout</response>
        /// <response code="500">Unexpected error occurred</response>
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
        /// Tests the connection to PostgreSQL and PostGIS.
        /// </summary>
        /// <remarks>
        /// <![CDATA[
        /// Returns a JSON with connection status:
        ///
        /// GOOD (PostGIS active):
        /// {
        ///   "success": bool,
        ///   "message": string,
        ///   "errors": List<string> or null,
        ///   "responseTime": string,
        ///   "canConnect": bool,
        ///   "postgisInstalled": bool,
        ///   "postgisVersion": string or null
        /// }
        ///
        /// BAD (Cannot connect or PostGIS missing):
        /// {
        ///   "success": bool,
        ///   "message": string,
        ///   "errors": List<string>,
        ///   "responseTime": string,
        ///   "canConnect": bool,
        ///   "postgisInstalled": bool,
        ///   "postgisVersion": string or null
        /// }
        /// ]]>
        /// </remarks>
        /// <response code="200">PostgreSQL connected and PostGIS active</response>
        /// <response code="503">Cannot connect to database</response>
        /// <response code="500">Unexpected error occurred</response>
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
