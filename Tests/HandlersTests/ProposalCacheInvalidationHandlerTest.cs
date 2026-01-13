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
    public class ProposalCacheInvalidationHandlerTest
    {
        [Fact]
        public async Task HandleAsync_WhenAccepted_InvalidatesUserStatsAndStationCache()
        {
            // Arrange
            Mock<IConnectionMultiplexer> redisMock = new Mock<IConnectionMultiplexer>();
            Mock<IDatabase> dbMock = new Mock<IDatabase>();
            Mock<IServer> serverMock = new Mock<IServer>();
            Mock<ILogger<CacheService>> loggerMock = new Mock<ILogger<CacheService>>();

            EndPoint endpoint = new DnsEndPoint("127.0.0.1", 6379);

            redisMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(dbMock.Object);
            redisMock
                .Setup(r => r.GetEndPoints(It.IsAny<bool>()))
                .Returns(new EndPoint[] { endpoint });
            redisMock
                .Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
                .Returns(serverMock.Object);

            RedisKey[] matchingKeys = new RedisKey[] { "key1", "key2" };
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

            CacheService cacheService = new CacheService(redisMock.Object, loggerMock.Object);
            ProposalCacheInvalidationHandler handler = new ProposalCacheInvalidationHandler(cacheService);

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com"
            };

            PriceProposal proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id
            };

            PriceProposalEvaluatedEvent @event = new PriceProposalEvaluatedEvent(proposal, true);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            dbMock.Verify(d => d.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k.ToString() == $"{CacheService.CacheKeys.UserStatsPrefix}{user.Email}"),
                    It.IsAny<CommandFlags>()),
                Times.Once);
            serverMock.Verify(s => s.Keys(
                    It.IsAny<int>(),
                    It.Is<RedisValue>(rv => rv.ToString() == $"{CacheService.CacheKeys.TopUsers}*"),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);

            serverMock.Verify(s => s.Keys(
                    It.IsAny<int>(),
                    It.Is<RedisValue>(rv => rv.ToString() == $"{CacheService.CacheKeys.StationPrefix}*"),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()),
                Times.AtLeastOnce);
        }

        [Fact]
        public async Task HandleAsync_WhenRejected_InvalidatesOnlyUserStatsCache()
        {
            // Arrange
            Mock<IConnectionMultiplexer> redisMock = new Mock<IConnectionMultiplexer>();
            Mock<IDatabase> dbMock = new Mock<IDatabase>();
            Mock<IServer> serverMock = new Mock<IServer>();
            Mock<ILogger<CacheService>> loggerMock = new Mock<ILogger<CacheService>>();

            EndPoint endpoint = new DnsEndPoint("127.0.0.1", 6379);

            redisMock
                .Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(dbMock.Object);
            redisMock
                .Setup(r => r.GetEndPoints(It.IsAny<bool>()))
                .Returns(new EndPoint[] { endpoint });
            redisMock
                .Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
                .Returns(serverMock.Object);

            RedisKey[] matchingKeys = new RedisKey[] { "key1" };
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

            CacheService cacheService = new CacheService(redisMock.Object, loggerMock.Object);
            ProposalCacheInvalidationHandler handler = new ProposalCacheInvalidationHandler(cacheService);

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com"
            };

            PriceProposal proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id
            };

            PriceProposalEvaluatedEvent @event = new PriceProposalEvaluatedEvent(proposal, false);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            dbMock.Verify(d => d.KeyDeleteAsync(
                    It.Is<RedisKey>(k => k.ToString() == $"{CacheService.CacheKeys.UserStatsPrefix}{user.Email}"),
                    It.IsAny<CommandFlags>()),
                Times.Once);

        
            serverMock.Verify(s => s.Keys(
                    It.IsAny<int>(),
                    It.Is<RedisValue>(rv => rv.ToString() == $"{CacheService.CacheKeys.StationPrefix}*"),
                    It.IsAny<int>(),
                    It.IsAny<long>(),
                    It.IsAny<int>(),
                    It.IsAny<CommandFlags>()),
                Times.Never);
        }
    }
}
