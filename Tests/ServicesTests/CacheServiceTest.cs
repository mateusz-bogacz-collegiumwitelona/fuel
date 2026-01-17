using System;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Services.Helpers;
using Xunit;

namespace Tests.ServicesTests
{
    public class CacheServiceTest
    {
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IServer> _serverMock;
        private readonly CacheService _cache;

        public CacheServiceTest()
        {
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _serverMock = new Mock<IServer>(MockBehavior.Loose);

            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            _redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(_serverMock.Object);

            var logger = Mock.Of<ILogger<CacheService>>();
            _cache = new CacheService(_redisMock.Object, logger);
        }

        [Fact]
        public async Task GetOrSetAsync_ReturnsCachedValue_WhenPresent()
        {
            // Arrange
            var key = "test:key";
            var expected = "cached-value";
            var serialized = System.Text.Json.JsonSerializer.Serialize(expected);
            _dbMock.Setup(d => d.StringGetAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(new RedisValue(serialized));

            
            Func<Task<string>> factory = () => throw new InvalidOperationException("Factory should not be called");

            // Act
            var result = await _cache.GetOrSetAsync(key, factory);

            // Assert
            Assert.Equal(expected, result);
        }

        [Fact]
        public async Task GetOrSetAsync_SetsCache_WhenMissing()
        {
            // Arrange
            var key = "test:miss";
            _dbMock.Setup(d => d.StringGetAsync(key, It.IsAny<CommandFlags>())).ReturnsAsync(RedisValue.Null);

            var value = "fresh-value";
            Func<Task<string>> factory = () => Task.FromResult(value);

            
            _dbMock.Setup(d => d.StringSetAsync(
                key,
                It.IsAny<RedisValue>(),
                It.IsAny<TimeSpan?>(),
                It.IsAny<bool>(),
                It.IsAny<When>(),
                It.IsAny<CommandFlags>()))
                .ReturnsAsync(true)
                .Verifiable();

            // Act
            var result = await _cache.GetOrSetAsync(key, factory, TimeSpan.FromMinutes(1));

            // Assert
            Assert.Equal(value, result);
            _dbMock.Verify();
        }

        [Fact]
        public async Task RemoveByPatternAsync_DeletesKeys_WhenFound()
        {
            // Arrange
            var pattern = "prefix:*";
            var keys = new[] { (RedisKey)"prefix:1", (RedisKey)"prefix:2" };
            _serverMock.Setup(s => s.Keys(
                It.IsAny<int>(),
                It.IsAny<RedisValue>(),
                It.IsAny<int>(),    
                It.IsAny<long>(),   
                It.IsAny<int>(),    
                It.IsAny<CommandFlags>()))
                .Returns(keys.Select(k => k).AsEnumerable());

            _dbMock.Setup(d => d.KeyDeleteAsync(It.Is<RedisKey[]>(rk => rk.Length == 2), It.IsAny<CommandFlags>()))
                .ReturnsAsync(2)
                .Verifiable();

            // Act
            await _cache.RemoveByPatternAsync(pattern);

            // Assert
            _dbMock.Verify();
        }
    }
}