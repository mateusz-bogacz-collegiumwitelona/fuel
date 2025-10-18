using Data.Context;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using NetTopologySuite.Geometries;
using StackExchange.Redis;
using System.Net.WebSockets;

namespace contlollers.Controllers.Test
{
    [ApiController]
    [Route("api/test")]
    public class TestContloller : ControllerBase
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<TestContloller> _logger;
        private readonly ApplicationDbContext _context;
        public TestContloller(
            IConnectionMultiplexer redis,
            ILogger<TestContloller> logger,
            ApplicationDbContext context)
        {
            _redis = redis;
            _logger = logger;
            _context = context;
        }

        [HttpGet("redis/ping")]
        public async Task<IActionResult> TestRedisPing()
        {
            try
            {
                var db = _redis.GetDatabase();
                var time = await db.PingAsync();

                return Ok(new
                {
                    status = "Connected",
                    responseTime = $"{time.TotalMilliseconds}ms",
                    endpoints = _redis.GetEndPoints().Select(e => e.ToString())
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Redis connection failed");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet("postgis/ping")]
        public async Task<IActionResult> TestPostisPing()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                if (!canConnect)
                    return StatusCode(500, new { error = "Cannot connect to PostgreSQL database" });

                return Ok(new { message = $"Connected to PostGIS"});
            }
            catch (Exception e)
            {
                return StatusCode(500, new { error = e.Message, inner = e.InnerException?.Message });
            }
        }

    }
}
