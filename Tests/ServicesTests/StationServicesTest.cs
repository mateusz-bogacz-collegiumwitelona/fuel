using Data.Interfaces;
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
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class StationServicesTest
    {
        private readonly Mock<IStationRepository> _stationRepoMock;
        private readonly Mock<IBrandRepository> _brandRepoMock;
        private readonly Mock<IFuelTypeRepository> _fuelTypeRepoMock;
        private readonly Mock<ILogger<StationServices>> _loggerMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IServer> _serverMock;
        private readonly CacheService _cache;
        private readonly StationServices _service;
        private readonly ITestOutputHelper _output;

        public StationServicesTest(ITestOutputHelper output)
        {
            _output = output;

            _stationRepoMock = new Mock<IStationRepository>();
            _brandRepoMock = new Mock<IBrandRepository>();
            _fuelTypeRepoMock = new Mock<IFuelTypeRepository>();
            _loggerMock = new Mock<ILogger<StationServices>>();

            // Configure minimal Redis mocks for CacheService to work like in other tests
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _serverMock = new Mock<IServer>(MockBehavior.Loose);

            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            var endpoint = new System.Net.DnsEndPoint("127.0.0.1", 6379);
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            _redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(_serverMock.Object);
            _serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());

            _cache = new CacheService(_redisMock.Object, Mock.Of<ILogger<CacheService>>());

            _service = new StationServices(
                _stationRepoMock.Object,
                _loggerMock.Object,
                _brandRepoMock.Object,
                _fuelTypeRepoMock.Object,
                _cache
            );
        }

        [Fact]
        public async Task GetAllStationsForMapAsync_ReturnsNotFound_WhenNoStations()
        {
            // Arrange
            var request = new GetStationsRequest();
            _stationRepoMock.Setup(r => r.GetAllStationsForMapAsync(It.IsAny<GetStationsRequest>()))
                .ReturnsAsync(new List<GetStationsResponse>());

            // Act
            var result = await _service.GetAllStationsForMapAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("GetAllStationsForMapAsync returns NotFound when repository returns empty list.");
        }

        [Fact]
        public async Task GetNearestStationAsync_ReturnsBadRequest_WhenLatitudeInvalid()
        {
            // Arrange
            double badLatitude = 100; // invalid
            double longitude = 0;

            // Act
            var result = await _service.GetNearestStationAsync(badLatitude, longitude, 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("GetNearestStationAsync returns BadRequest for invalid latitude.");
        }

        [Fact]
        public async Task GetNearestStationAsync_ReturnsNotFound_WhenRepositoryReturnsEmpty()
        {
            // Arrange
            _stationRepoMock.Setup(r => r.GetNearestStationAsync(It.IsAny<double>(), It.IsAny<double>(), It.IsAny<int?>()))
                .ReturnsAsync(new List<GetStationsResponse>());

            // Act
            var result = await _service.GetNearestStationAsync(10, 10, 1);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("GetNearestStationAsync returns NotFound when repository returns no nearest stations.");
        }

        [Fact]
        public async Task GetStationListAsync_ReturnsBadRequest_WhenInvalidFuelTypesProvided()
        {
            // Arrange
            var request = new GetStationListRequest
            {
                FuelType = new List<string> { "BAD_FUEL" }
            };

            _fuelTypeRepoMock.Setup(r => r.GetAllFuelTypeCodesAsync())
                .ReturnsAsync(new List<string> { "ON", "LPG" });

            // Act
            var result = await _service.GetStationListAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("Invalid fuel type(s)", result.Errors?.FirstOrDefault() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("GetStationListAsync validates fuel types and returns BadRequest for invalid ones.");
        }

        [Fact]
        public async Task GetStationListAsync_ReturnsBadRequest_WhenBrandDoesNotExist()
        {
            // Arrange
            var request = new GetStationListRequest
            {
                BrandName = "NonExistingBrand"
            };

            _brandRepoMock.Setup(r => r.FindBrandAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _service.GetStationListAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("GetStationListAsync returns BadRequest when brand doesn't exist.");
        }

        [Fact]
        public async Task GetStationProfileAsync_ReturnsBadRequest_WhenBrandEmpty()
        {
            // Arrange
            var request = new FindStationRequest
            {
                BrandName = "",
                Street = "S",
                HouseNumber = "1",
                City = "C"
            };

            // Act
            var result = await _service.GetStationProfileAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("GetStationProfileAsync returns BadRequest when BrandName is empty.");
        }

        [Fact]
        public async Task AddNewStationAsync_ReturnsBadRequest_WhenBrandDoesNotExist()
        {
            // Arrange
            var request = new AddStationRequest
            {
                BrandName = "NoBrand",
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest { Code = "ON", Name = "Diesel", Price = 5m }
                }
            };

            _brandRepoMock.Setup(r => r.FindBrandAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _service.AddNewStationAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("AddNewStationAsync returns BadRequest when brand does not exist.");
        }

        [Fact]
        public async Task AddNewStationAsync_ReturnsBadRequest_WhenFuelTypesContainInvalidCodes()
        {
            // Arrange
            var request = new AddStationRequest
            {
                BrandName = "Brand1",
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    new AddFuelTypeAndPriceRequest { Code = "BAD", Name = "BadFuel", Price = 5m }
                }
            };

            _brandRepoMock.Setup(r => r.FindBrandAsync(It.IsAny<string>())).ReturnsAsync(true);
            _fuelTypeRepoMock.Setup(r => r.GetAllFuelTypeCodesAsync()).ReturnsAsync(new List<string> { "ON", "LPG" });

            // Act
            var result = await _service.AddNewStationAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("AddNewStationAsync returns BadRequest when fuel type codes are invalid.");
        }

        [Fact]
        public async Task EditStationAsync_ReturnsBadRequest_WhenNewBrandDoesNotExist()
        {
            // Arrange
            var request = new EditStationRequest
            {
                NewBrandName = "NonExistingBrand",
                FindStation = new FindStationRequest
                {
                    BrandName = "Brand1",
                    City = "C",
                    Street = "S",
                    HouseNumber = "1"
                }
            };

            _brandRepoMock.Setup(r => r.FindBrandAsync(It.IsAny<string>())).ReturnsAsync(false);

            // Act
            var result = await _service.EditStationAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("EditStationAsync returns BadRequest when new brand is invalid.");
        }
    }
}
