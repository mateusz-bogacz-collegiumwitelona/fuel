using Data.Context;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.IO;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class StationRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<StationRepository>> _loggerMock;
        private readonly StationRepository _repository;
        private readonly ITestOutputHelper _output;
        private readonly Mock<IFuelTypeRepository> _fuelMock;

        public StationRepositoryTest(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var _brandRepo = new Mock<IBrandRepository>();
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<StationRepository>>();
            _fuelMock = new Mock<IFuelTypeRepository>();

            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);

            var brand1 = new Brand { Id = new Guid(), Name = "Brand1", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            var brand2 = new Brand { Id = new Guid(), Name = "Brand2", CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow };
            _context.Brand.AddRange(brand1, brand2);
            var diesel = new FuelType { Id = Guid.NewGuid(), Name = "Diesel", Code = "ON" };
            var lpg = new FuelType { Id = Guid.NewGuid(), Name = "Lpg gas", Code = "LPG" };
            _context.FuelTypes.AddRange(diesel, lpg);

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

            _context.FuelPrices.AddRange(
                new FuelPrice { StationId = Station1.Id, FuelType = diesel, Price = 6 },
                new FuelPrice { StationId = Station2.Id, FuelType = lpg, Price = 3 });

            _context.SaveChanges();
            _repository = new StationRepository(_context, _loggerMock.Object, _brandRepo.Object, _fuelMock.Object);
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_NoFilter_SuccessIfStationsReturned()
        {
            //Arrange
            var request = new GetStationsRequest();

            //Act
            var result = await _repository.GetAllStationsForMapAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            _output.WriteLine("Success, GetAllStationsForMapAsync returns all available stations");
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_BrandFilter_SuccessIfBrand2Returned()
        {
            //Arrange
            var request = new GetStationsRequest
            {
                BrandName = new List<string> { "Brand2" }
            };

            //Act
            var result = await _repository.GetAllStationsForMapAsync(request);

            //Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetAllStationsForMapAsync returns the correct brand using the brand filter");
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_DistanceFilter_SuccessIfCorrectOrderReturned()
        {
            //Arrange
            var requestBrand1First = new GetStationsRequest
            {
                Distance = 15000,
                LocationLatitude = 10,
                LocationLongitude = 10,
            };
            var requestBrand2First = new GetStationsRequest
            {
                Distance = 15000,
                LocationLatitude = 20,
                LocationLongitude = 20,
            };

            //Act
            var result1 = await _repository.GetAllStationsForMapAsync(requestBrand1First);
            var result2 = await _repository.GetAllStationsForMapAsync(requestBrand2First);

            //Assert
            Assert.Equal(2, result1.Count());
            Assert.Equal("Brand1", result1.ToList().First().BrandName);
            Assert.Equal("Brand2", result1.ToList().Skip(1).First().BrandName);

            Assert.Equal(2, result2.Count());
            Assert.Equal("Brand1", result2.ToList().First().BrandName);
            Assert.Equal("Brand2", result2.ToList().Skip(1).First().BrandName);
            _output.WriteLine("Success, GetAllStationsForMapAsync returns the correct stations when using the distance filter");
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_StationCheck_SuccessIfCorrectStationInfoReturned()
        {
            //Arrange
            var request = new GetStationsRequest();

            //Act
            var result = await _repository.GetAllStationsForMapAsync(request);
            var station = result.ToList().First();
            var station2 = result.ToList().Skip(1).First();

            //Assert
            Assert.Equal("TestStreet1", station.Street);
            Assert.Equal("1", station.HouseNumber);
            Assert.Equal("TestCity1", station.City);
            Assert.Equal("00-001", station.PostalCode);
            Assert.Equal(10.0, station.Latitude);
            Assert.Equal(10.0, station.Longitude);

            Assert.Equal("TestStreet2", station2.Street);
            Assert.Equal("2", station2.HouseNumber);
            Assert.Equal("TestCity2", station2.City);
            Assert.Equal("00-002", station2.PostalCode);
            Assert.Equal(20.0, station2.Latitude);
            Assert.Equal(20.0, station2.Longitude);
            _output.WriteLine("Success, GetAllStationsForMapAsync returns correct data for stations");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_SuccessIfBrand2Returned()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.GetNearestStationAsync(19, 19, 1);

            //Assert
            Assert.Equal("Brand2", result.First().BrandName);
            _output.WriteLine("Success, GetNearestStationAsync returns the nearest station");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_SuccessIfBrand2ThenBrand1Returned()
        {
            //Arrange
            //- 

            //Act
            var result = await _repository.GetNearestStationAsync(16, 16, null);

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("Brand2", result.First().BrandName);
            Assert.Equal("Brand1", result.Skip(1).First().BrandName);
            _output.WriteLine("Success, GetNearestStationAsync returns 2 stations (3 by default but our repo has been seeded with 2 stations) in the correct order when count is null");
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_NegativeCount_SuccessIfReturnsBothBrands()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.GetNearestStationAsync(1, 1, -100);

            //Assert
            Assert.Equal(2, result.Count());
            _output.WriteLine("Success, GetNearestStationAsync returns defaults to 3 closest stations when count is negative (2 in our case, read comment of the test above");
        }

        [Fact]
        public async Task GetStationListAsyncTest_NoFilter_SuccessIfAllReturned()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                SortingByDisance = null,
                SortingByPrice = null,
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(2, result.Count());
            _output.WriteLine("Success, GetStationListAsync returns all stations");
        }

        [Fact]
        public async Task GetStationListAsyncTest_FilterByDistance_SuccessIfBrand2Returned()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                LocationLatitude = 19.9,
                LocationLongitude = 19.9,
                Distance = 500
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListAsync returns correct station when using the distance filter");
        }

        [Fact]
        public async Task GetStationListAsyncTest_FuelTypeFilter_SuccessIfReturnsLPG_Station2()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                SortingByDisance = null,
                SortingByPrice = null,
                FuelType = new List<string> { "Lpg gas" }
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Brand2", result.First().BrandName);
            _output.WriteLine("Success, GetStationListAsync returns correct station when using the fuel type filter");
        }

        [Fact]
        public async Task GetStationListAsyncTest_PriceFilter_SuccessIfReturnsStation1()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                SortingByDisance = null,
                SortingByPrice = null,
                MinPrice = 4m,
                MaxPrice = 6.2m
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Brand1", result.ToList().First().BrandName);
            _output.WriteLine("Sucecss, GetStationListAsync works correctly with price filter");
        }

        [Fact]
        public async Task GetStationListAsyncTest_BrandFilter_SuccessIfReturnsStation2()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                SortingByDisance = null,
                SortingByPrice = null,
                BrandName = "Brand2"
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(1, result.Count());
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListAsync returns correct station with a brand filter");
        }

        [Fact]
        public async Task GetStationListAsyncTest_PriceSortAsc_SuccessIfReturnsSt2ThenSt1()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                SortingByDisance = null,
                SortingByPrice = true,
                SortingDirection = "Asc"
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            Assert.Equal("Brand1", result.ToList().Skip(1).First().BrandName);
            _output.WriteLine("Success, GetStationListAsync orders stations by price correctly");
        }

        [Fact]
        public async Task GetStationListAsyncTest_DistSortAsc_SuccessIfReturnsSt2ThenSt1()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                LocationLatitude = 18,
                LocationLongitude = 19,
                SortingByDisance = true,
                SortingByPrice = null,
                SortingDirection = "Asc"
            };

            //Act
            var result = await _repository.GetStationListAsync(request);

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            Assert.Equal("Brand1", result.ToList().Skip(1).First().BrandName);
            _output.WriteLine("Success, GetStationListAsync orders stations by distance correctly");
        }
    }
}
