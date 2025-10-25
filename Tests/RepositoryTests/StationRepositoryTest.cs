using Data.Context;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Services.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class StationRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<StationRepository>> _loggerMock;
        private readonly StationRepository _repository;
        private readonly ITestOutputHelper _output;

        public StationRepositoryTest(ITestOutputHelper output)
        {
            // test output setup
            _output = output;

            // inMemory db setup
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<StationRepository>>();

            // geofac setup
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            // creating test brands
            var brand1 = new Brand { Id = new Guid(), Name = "Brand1", LogoUrl = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var brand2 = new Brand { Id = new Guid(), Name = "Brand2", LogoUrl = "", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Brand.AddRange(brand1, brand2);

            // creating addresses for stations
            var address1 = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "TestStreet1",
                HouseNumber = "1",
                City = "TestCity1",
                PostalCode = "00-001",
                Location = geometryFactory.CreatePoint(new Coordinate(10.0, 10.0))
            };

            var address2 = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "TestStreet2",
                HouseNumber = "2",
                City = "TestCity2",
                PostalCode = "00-002",
                Location = geometryFactory.CreatePoint(new Coordinate(20.0, 20.0))
            };
            _context.StationAddress.AddRange(address1, address2);

            //creating stations
            var Station1 = new Station
            {
                Id = Guid.NewGuid(),
                BrandId = brand1.Id,
                Brand = brand1,
                AddressId = address1.Id,
                Address = address1,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            var Station2 = new Station
            {
                Id = Guid.NewGuid(),
                BrandId = brand2.Id,
                Brand = brand2,
                AddressId = address2.Id,
                Address = address2,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Stations.AddRange(Station1, Station2);

            _context.SaveChanges();
            // repo setup
            _repository = new StationRepository(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_NoFilter_SuccessIfReturnsAll()
        {
            // Arrange
            //
            // Act
            // a request with no filters 
            var request = new GetStationsRequest();
            var result = await _repository.GetAllStationsForMapAsync(request);

            // Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, s => s.BrandName == "Brand1");
            Assert.Contains(result, s => s.BrandName == "Brand2");
            _output.WriteLine("Test passed: GetAllStationsForMapAsyncTest() returns all stations");
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_BrandNameFilter_SuccessIfReturnsStation1()
        { 
            // Arrange
            //

            // Act
            // sending a request with a filter on
            var request = new GetStationsRequest
            {
                BrandName = new List<string> { "Brand1" }
            };
            var result = await _repository.GetAllStationsForMapAsync(request);

            //Assert
            Assert.Single(result);
            Assert.Equal("Brand1", result.First().BrandName);
            _output.WriteLine("Test passed: GetAllStationsForMapAsyncTest() returns a Brand1 station");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_OneStation_SuccessWhenReturnsNearest()
        {
            // Arrange
            // setting up users coordinates and a request
            double userX = 10.0;
            double userY = 10.0;
            int distance = 10000;

            var request = new GetStationsRequest
            {
                LocationLatitude = userX,
                LocationLongitude = userY,
                Distance = distance
            };

            // Act
            var result = await _repository.GetAllStationsForMapAsync(request);

            // Assert
            Assert.Equal("TestStreet1", result[0].Street);
            Assert.Equal("Brand1", result[0].BrandName);
            _output.WriteLine("Test passed: GetNearestStationAsync() returns the closest Brand1");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_TwoStation_SuccessWhenReturnsNearestOfBrand2()
        {
            // Arrange
            // setting up users coordinates and a request
            double userX = 10.0;
            double userY = 10.0;
            int distance = 10000;

            var request = new GetStationsRequest
            {
                BrandName = new List<string> { "Brand2" },
                LocationLatitude = userX,
                LocationLongitude = userY,
                Distance = distance
            };

            // Act
            var result = await _repository.GetAllStationsForMapAsync(request);

            // Assert
            Assert.Single(result);
            Assert.Equal("TestStreet2", result[0].Street);
            Assert.Equal("Brand2", result[0].BrandName);
            _output.WriteLine("Test passed: GetNearestStationAsync() returns the closest Brand2");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_IncorrectBrand_SuccessWhenReturnsEmpty()
        {
            // Arrange
            double userX = 10.0;
            double userY = 10.0;
            int distance = 10000;
            var request = new GetStationsRequest
            {
                BrandName = new List<string> { "Brand3" },
                LocationLatitude = userX,
                LocationLongitude = userY,
                Distance = distance
            };

            // Act
            var result = await _repository.GetAllStationsForMapAsync(request);

            // Asser
            Assert.Empty(result);
            _output.WriteLine("Test passed: GetNearestStationAsync() returns empty");
        }
    }
}
