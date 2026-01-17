using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Helpers;
using Services.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class ProposalStatisticServicesTest
    {
        private readonly Mock<IProposalStatisticRepository> _repoMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IServer> _serverMock;
        private readonly CacheService _cache;
        private readonly Mock<ILogger<ProposalStatisticServices>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly ProposalStatisticServices _service;
        private readonly ITestOutputHelper _output;

        public ProposalStatisticServicesTest(ITestOutputHelper output)
        {
            _output = output;
            _repoMock = new Mock<IProposalStatisticRepository>();
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _serverMock = new Mock<IServer>(MockBehavior.Loose);
            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            _redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(_serverMock.Object);
            _serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());
            _cache = new CacheService(_redisMock.Object, Mock.Of<ILogger<CacheService>>());

            _loggerMock = new Mock<ILogger<ProposalStatisticServices>>();
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };
            _repoMock.Setup(r => r.GetTopUserListAsync()).ReturnsAsync(new List<TopUserResponse>());

            _service = new ProposalStatisticServices(
                _repoMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object,
                _cache
            );
        }

        [Fact]
        public async Task GetUserProposalStatisticResponse_ReturnsNotFound_WhenUserDoesNotExist()
        {
            // Arrange
            var email = "missing@example.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.GetUserProposalStatisticResponse(email);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            _output.WriteLine("Test passed: GetUserProposalStatisticResponse returns NotFound when user is missing");
        }

        [Fact]
        public async Task GetUserProposalStatisticResponse_ReturnsError_WhenRepositoryThrows()
        {
            // Arrange
            var email = "user@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };
            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
            _repoMock.Setup(r => r.GetUserProposalStatisticAsync(user)).ThrowsAsync(new Exception("repo failure"));

            // Act
            var result = await _service.GetUserProposalStatisticResponse(email);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.NotEqual(200, result.StatusCode);
            _output.WriteLine("Test passed: GetUserProposalStatisticResponse returns error when repository throws");
        }

        [Fact]
        public async Task GetUserProposalStatisticResponse_ReturnsSuccess_WhenRepositoryReturnsData()
        {
            // Arrange
            var email = "ok@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };
            var expected = new GetProposalStatisticResponse(); 

            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
            _repoMock.Setup(r => r.GetUserProposalStatisticAsync(user)).ReturnsAsync(expected);

            // Act
            var result = await _service.GetUserProposalStatisticResponse(email);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(expected, result.Data);
            _output.WriteLine("Test passed: GetUserProposalStatisticResponse returns success and data");
        }
    }
}

