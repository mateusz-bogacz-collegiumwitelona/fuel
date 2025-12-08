using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
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
using Data.Models;
using Data.Interfaces;
using Xunit;

namespace Tests.ServicesTests
{
    public class FuelTypeServiceTest
    {
        private readonly Mock<IFuelTypeRepository> _repoMock;
        private readonly Mock<IStationRepository> _stationRepoMock;
        private readonly Mock<ILogger<FuelTypeServices>> _loggerMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IServer> _serverMock;
        private readonly Mock<ILogger<CacheService>> _cacheLoggerMock;
        private readonly CacheService _cache;
        private readonly FuelTypeServices _service;

        public FuelTypeServiceTest()
        {
            _repoMock = new Mock<IFuelTypeRepository>();
            _stationRepoMock = new Mock<IStationRepository>();
            _loggerMock = new Mock<ILogger<FuelTypeServices>>();
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _serverMock = new Mock<IServer>(MockBehavior.Loose);
            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            _redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(_serverMock.Object);
            _serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());
            _cacheLoggerMock = new Mock<ILogger<CacheService>>();
            _cache = new CacheService(_redisMock.Object, _cacheLoggerMock.Object);
            _repoMock.Setup(r => r.FindFuelTypeByCodeAsync(It.IsAny<string>())).ReturnsAsync((FuelType?)null);
            _repoMock.Setup(r => r.AddFuelTypeAsync(It.IsAny<string>(), It.IsAny<string>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.DeleteFuelTypeAsync(It.IsAny<FuelType>())).ReturnsAsync(false);
            _repoMock.Setup(r => r.GetAllFuelTypeCodesAsync()).ReturnsAsync(new List<string>());
            _repoMock.Setup(r => r.GetFuelsTypeListAsync(It.IsAny<TableRequest>())).ReturnsAsync(new List<GetFuelTypeResponses>());

            _service = new FuelTypeServices(
                _repoMock.Object,
                _loggerMock.Object,
                _cache,
                _stationRepoMock.Object
            );
        }

        [Fact]
        public async Task GetAllFuelTypeCodes_ReturnsNotFound_WhenEmpty()
        {
            // Arrange
            _repoMock.Setup(r => r.GetAllFuelTypeCodesAsync()).ReturnsAsync(new List<string>());

            // Act
            var result = await _service.GetAllFuelTypeCodesAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public async Task GetAllFuelTypeCodes_ReturnsList_WhenPresent()
        {
            // Arrange
            var codes = new List<string> { "D", "PB95" };
            _repoMock.Setup(r => r.GetAllFuelTypeCodesAsync()).ReturnsAsync(codes);

            // Act
            var result = await _service.GetAllFuelTypeCodesAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(2, result.Data?.Count);
        }

        [Fact]
        public async Task GetFuelsTypeList_ReturnsPagedResult_WhenRepoReturnsItems()
        {
            // Arrange
            var items = new List<GetFuelTypeResponses>
            {
                new GetFuelTypeResponses { Name = "Diesel", Code = "D" },
                new GetFuelTypeResponses { Name = "Petrol", Code = "PB95" }
            };
            _repoMock.Setup(r => r.GetFuelsTypeListAsync(It.IsAny<TableRequest>())).ReturnsAsync(items);

            var pagged = new GetPaggedRequest { PageNumber = 1, PageSize = 10 };
            var request = new TableRequest();

            // Act
            var result = await _service.GetFuelsTypeListAsync(pagged, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(2, result.Data.Items.Count);
        }

        [Fact]
        public async Task AddFuelTypeAsync_ReturnsBadRequest_WhenInvalidInput()
        {
            // Arrange 
            var badRequest = new AddFuelTypeRequest { Name = "Diesel", Code = "" };

            // Act
            var result = await _service.AddFuelTypeAsync(badRequest);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task AddFuelTypeAsync_ReturnsConflict_WhenCodeExists()
        {
            // Arrange
            var request = new AddFuelTypeRequest { Name = "Diesel", Code = "D" };
            _repoMock.Setup(r => r.FindFuelTypeByCodeAsync("D")).ReturnsAsync(new FuelType { Id = Guid.NewGuid(), Code = "D", Name = "Diesel" });

            // Act
            var result = await _service.AddFuelTypeAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Fact]
        public async Task AddFuelTypeAsync_Successful_AddsAndInvalidatesCache()
        {
            // Arrange
            var request = new AddFuelTypeRequest { Name = "Diesel", Code = "D" };
            _repoMock.Setup(r => r.FindFuelTypeByCodeAsync("D")).ReturnsAsync((FuelType?)null);
            _repoMock.Setup(r => r.AddFuelTypeAsync("Diesel", "D")).ReturnsAsync(true);

            // Act
            var result = await _service.AddFuelTypeAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);

            // Cache invalidation should call KeyDelete on underlying IDatabase
            Assert.True(_dbMock.Invocations.Any(inv => inv.Method.Name.IndexOf("KeyDelete", StringComparison.OrdinalIgnoreCase) >= 0),
                "Expected cache key delete to be invoked during fuel type cache invalidation.");
        }

        [Fact]
        public async Task DeleteFuelTypeAsync_ReturnsNotFound_WhenMissing()
        {
            // Arrange
            _repoMock.Setup(r => r.FindFuelTypeByCodeAsync("X")).ReturnsAsync((FuelType?)null);

            // Act
            var result = await _service.DeleteFuelTypeAsync("X");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public async Task DeleteFuelTypeAsync_Successful_DeletesAndInvalidatesCache()
        {
            // Arrange
            var ft = new FuelType { Id = Guid.NewGuid(), Name = "Diesel", Code = "D" };
            _repoMock.Setup(r => r.FindFuelTypeByCodeAsync("D")).ReturnsAsync(ft);
            _repoMock.Setup(r => r.DeleteFuelTypeAsync(ft)).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteFuelTypeAsync("D");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

            Assert.True(_dbMock.Invocations.Any(inv => inv.Method.Name.IndexOf("KeyDelete", StringComparison.OrdinalIgnoreCase) >= 0),
                "Expected cache key delete to be invoked during fuel type cache invalidation.");
        }
    }
}

