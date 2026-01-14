using System;
using System.Net;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Handlers;
using Services.Helpers;
using StackExchange.Redis;
using Xunit;

namespace Tests.HandlersTests
{
    public class InvalidateUnlockedUserCacheHandlerTest
    {
        [Fact]
        public async Task HandleAsync_RemovesUsersListByPattern_AndInvalidatesUserInfoCache()
        {
            // Arrange
            var redisMock = new Mock<IConnectionMultiplexer>();
            var dbMock = new Mock<IDatabase>();
            var serverMock = new Mock<IServer>();
            var loggerMock = new Mock<ILogger<CacheService>>();

            var endpoint = new DnsEndPoint("127.0.0.1", 6379);

            redisMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(dbMock.Object);

            redisMock
                .Setup(r => r.GetEndPoints(It.IsAny<bool>()))
                .Returns(new EndPoint[] { endpoint });

            redisMock
                .Setup(r => r.GetServer(It.IsAny<EndPoint>(), null))
                .Returns(serverMock.Object);

            var matchingKeys = new RedisKey[] { "users:list:1", "users:list:2" };
            serverMock
                .Setup(s => s.Keys(
                    It.IsAny<int>(),
                    It.IsAny<RedisValue>(),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()))
                .Returns(matchingKeys);

            dbMock
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync((long)matchingKeys.Length);

            dbMock
                .Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);

            var cacheService = new CacheService(redisMock.Object, loggerMock.Object);
            var handler = new InvalidateBannedUserCacheHandler(cacheService);

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "banned@example.com"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin"
            };

            var @event = new UserBannedEvent(user, admin, "reason", 3);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            
            serverMock.Verify(s => s.Keys(
                    It.IsAny<int>(),
                    It.Is<RedisValue>(rv => rv.ToString() == $"{CacheService.CacheKeys.UsersList}*"),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);

            dbMock.Verify(d => d.KeyDeleteAsync(
                    It.Is<RedisKey[]>(keys => keys.Length == matchingKeys.Length),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);

            dbMock.Verify(d => d.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k.ToString() == $"{CacheService.CacheKeys.UserInfoPrefix}{user.Email}"),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);

            dbMock.Verify(d => d.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k.ToString() == $"{CacheService.CacheKeys.UserStatsPrefix}{user.Email}"),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);
        }
    }
}
