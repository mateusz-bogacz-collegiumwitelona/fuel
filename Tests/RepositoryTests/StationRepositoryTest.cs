using Data.Context;
using Data.Interfaces;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Services.Helpers;
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
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
            
            _context = new ApplicationDbContext(options);
            var _brandMock = new Mock<ILogger<BrandRepository>>();
            var _brandRepoMock = new BrandRepository(_context, _brandMock.Object);
            _loggerMock = new Mock<ILogger<StationRepository>>();
            var _fuelRepoMock = new FuelTypeRepository(_context, _loggerMock.Object);

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
            _repository = new StationRepository(_context, _loggerMock.Object, _brandRepoMock, _fuelRepoMock);
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

        [Fact]
        public async Task GetStationProfileAsyncTest_SuccessWhenCorrectStationReturned()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "Brand1",
                HouseNumber = "1",
                City = "TestCity1",
                Street = "TestStreet1"
            };

            //Act
            var result = await _repository.GetStationProfileAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal("Brand1", result.BrandName);
            Assert.Equal("1", result.HouseNumber);
            Assert.Equal("TestCity1", result.City);
            Assert.Equal("TestStreet1", result.Street);
            Assert.Equal("ON", result.FuelPrice.First().FuelCode);
            _output.WriteLine("Success, GetStationProfile fetches correct station");
        }

        [Fact]
        public async Task GetStationProfileAsyncTest_StationDoesntExist_SuccessIfNull()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "BadBrand",
                HouseNumber = "123",
                City = "BadCity",
                Street = "BadStreet"
            };

            //Act
            var result = await _repository.GetStationProfileAsync(request);

            //Assert
            Assert.Null(result);
            _output.WriteLine("Success, GetStationProfileAsync returns null when requesting a non-existent station");
        }

        [Fact]
        public async Task FindStationByDataAsyncTest_NullStation_SuccessIfReturnsNull()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.FindStationByDataAsync("BadBrand", "BadStreet", "BadNumber", "BadCity");

            //Assert
            Assert.Null(result);
            _output.WriteLine("Success, FindStationByDataAsync retunrs null when requesting a non-existent station");
        }

        [Fact]
        public async Task FindStationByDataAsyncTest_SuccessWhenReturnsStation()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.FindStationByDataAsync("Brand1", "TestStreet1", "1", "TestCity1");

            //Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result.FuelPrice);
            _output.WriteLine("Success, FindStationByDataAsync returns the station and its fuelPrice");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_NoFilter_SuccessIfReturnsAll()
        {
            //Arrange
            var request = new TableRequest();

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            _output.WriteLine("Success, GetStationListForAdminAsync returns all stations with no filter");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_BrandFilter_SuccessIfReturnsBRand2()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "Brand2"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            _output.WriteLine("Success, GetStationListForAdminAsync returns correct station by searching through brands");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_StreetFilter_SuccessIfReturnsBrand2()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "TestStreet2"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListForAdminAsync returns correct station by searching through streets");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_HouseNumberFilter_SuccessIfReturnsBrand2()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "2"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListForAdminAsync returns correct station by searching through house numbers");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_CityFilter_SuccessIfReturnsBrand2()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "TestCity2"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListForAdminAsync returns correct station by searching through city names");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_PostalFilter_SuccessIfReturnsBrand2()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "00-002"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, result.Count);
            Assert.Equal("Brand2", result.ToList().First().BrandName);
            _output.WriteLine("Success, GetStationListForAdminAsync returns correct station by searching through postal codes");
        }

        [Fact]
        public async Task GetStationsListForAdminAsyncTest_BadFilter_SuccessIfReturnsEmpty()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "Bad search"
            };

            //Act
            var result = await _repository.GetStationsListForAdminAsync(request);

            //Assert
            Assert.Empty(result);
            _output.WriteLine("Success, GetStationListForAdminAsync returns empty when looking for non existent station");
        }

        [Fact]
        public async Task IsStationExistAsyncTest_DoesntExist_SuccessIfReturnsFalse()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.IsStationExistAsync("Bad", "Bad", "Bad", "Bad");

            //Assert
            Assert.False(result);
            _output.WriteLine("Success, IsStationExistAsync returns false when station doesn't exist");
        }

        [Fact]
        public async Task IsStationExistAsyncTest_GoodData_SuccessIfReturnsTrue()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.IsStationExistAsync("brand1","teststreet1", "1","testcity1");

            //Assert
            Assert.True(result);
            _output.WriteLine("Success, IsStationExistAsync returns true when station exists");
        }

        [Fact]
        public async Task EditStationAsyncTest_BadStation_SuccessIfReturnsFalse()
        {
            //Arrange
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "BadBrand",
                    Street = "BadStreet",
                    HouseNumber = "123123",
                    City = "BadCity"
                }
            };

            //Act
            var result = await _repository.EditStationAsync(request);

            //Assert
            Assert.False(result);
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Station not found for edit")),
                    It.IsAny<Exception>(),
                    It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _output.WriteLine("Success, EditStationAsync returns false when station is not found");
        }

        [Fact]
        public async Task EditStationAsyncTest_AddressUpdate_SuccessIfAddressUpdated()
        {
            //Arrange
            var stationToUpdate = await _context.Stations.Include(s => s.Address).FirstAsync();
            var oldAddress = stationToUpdate.Address;
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = stationToUpdate.Brand.Name,
                    Street = stationToUpdate.Address.Street,
                    HouseNumber = stationToUpdate.Address.HouseNumber,
                    City = stationToUpdate.Address.City,
                },
                NewCity = "NewCity",
                NewStreet = "NewStreet",
                NewHouseNumber = "100",
                NewBrandName = "Brand2",
                NewPostalCode = "00-100",
                NewLatitude = 15.0,
                NewLongitude = 15.0
            };

            //Act
            var result = await _repository.EditStationAsync(request);
            var updatedStation = await _context.Stations.Include(s => s.Address).Include(s => s.Brand).FirstAsync();

            //Assert
            Assert.True(result);
            Assert.Equal("NewCity", updatedStation.Address.City);
            Assert.Equal("NewStreet", updatedStation.Address.Street);
            Assert.Equal("100", updatedStation.Address.HouseNumber);
            Assert.Equal("Brand2", updatedStation.Brand.Name);
            Assert.Equal("00-100", updatedStation.Address.PostalCode);
            Assert.Equal(15.0, updatedStation.Address.Location.Y);
            Assert.Equal(15.0, updatedStation.Address.Location.X);
            _output.WriteLine("Success, EditStationAsync correctly edits the station info");
        }

        [Fact]
        public async Task EditStationAsyncTest_UpdateFuel_SuccessIfPriceUpdated()
        {
            //Arrange
            var stationToUpdate = await _context.Stations.Include(s => s.Address).FirstAsync();
            var dieselUpdatedPrice = new AddFuelTypeAndPriceRequest
            {
                Code = "ON",
                Price = 7.0m,
                Name = "Diesel"
            };
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Brand1",
                    City = "TestCity1",
                    HouseNumber = "1",
                    Street = "TestStreet1"
                },
                FuelType = new List<AddFuelTypeAndPriceRequest> { dieselUpdatedPrice },
            };

            //Act
            var result = await _repository.EditStationAsync(request);
            var updatedStation = await _context.Stations.Include(s => s.Address).FirstAsync();
            
            //Assert
            Assert.True(result);
            Assert.Equal(stationToUpdate, updatedStation);
            Assert.Equal(7.0m, _context.FuelPrices.ToList().First().Price);
            Assert.Equal(3, _context.FuelPrices.ToList().Skip(1).First().Price);
            _output.WriteLine("Success, EditStationAsync updates price of a certain fuel type for a certain station");
        }

        [Fact]
        public async Task EditStationAsyncTest_DeleteFuelTypeAddNewOne_SuccessIfFuelsSwapped()
        {
            //Arrange
            var stationToUpdate = await _context.Stations.Include(s => s.Address).FirstAsync();
            var dieselDeletedLPGAdded = new AddFuelTypeAndPriceRequest
            {
                Name = "Lpg gas",
                Code = "LPG",
                Price = 3.0m
            };
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Brand1",
                    City = "TestCity1",
                    HouseNumber = "1",
                    Street = "TestStreet1"
                },
                FuelType = new List<AddFuelTypeAndPriceRequest> { dieselDeletedLPGAdded },
            };

            //Act
            var result = await _repository.EditStationAsync(request);
            var updatedStation = await _context.Stations.Include(s => s.Address).FirstAsync();

            //Assert
            Assert.Single(updatedStation.FuelPrice.ToList());
            Assert.Equal("Lpg gas", updatedStation.FuelPrice.ToList().First().FuelType.Name);
            Assert.Equal(3.0m, updatedStation.FuelPrice.ToList().First().Price);
            _output.WriteLine("Success, EditStationAsync correctly deletes and adds new fuel types to station.");
        }

        [Fact]
        public async Task EditStationAsyncTest_BadFuel_SuccessIfException()
        {
            //Arrange
            var stationToUpdate = await _context.Stations.Include(s => s.Address).FirstAsync();
            var dieselDeletedLPGAdded = new AddFuelTypeAndPriceRequest
            {
                Name = "LPG gas",
                Code = "BadCode",
                Price = 3.0m
            };
            var request = new EditStationRequest
            {
                FindStation = new FindStationRequest
                {
                    BrandName = "Brand1",
                    City = "TestCity1",
                    HouseNumber = "1",
                    Street = "TestStreet1"
                },
                FuelType = new List<AddFuelTypeAndPriceRequest> { dieselDeletedLPGAdded },
            };

            //Act n Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.EditStationAsync(request));
            _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Error editing station")),
                It.IsAny<InvalidOperationException>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
        }

        [Fact]
        public async Task GetStationInfoForEditTest_BadStation_SuccessIfReturnsNull()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "BadBrand",
                City = "BadCity",
                HouseNumber = "BadNumber",
                Street = "BadStreet"
            };

            //Act
            var result = await _repository.GetStationInfoForEdit(request);

            //Assert
            Assert.Null(result);
            _output.WriteLine("Success, GetStationInfoForEdit returns null when station doesn't exist");
        }

        [Fact]
        public async Task GetStationInfoForEditTest_StationExist_SuccessIfReturnsStation2()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "Brand2",
                City = "TestCity2",
                Street = "TestStreet2",
                HouseNumber = "2"
            };

            //Act
            var result = await _repository.GetStationInfoForEdit(request);

            //Assert
            Assert.NotNull(result);
            Assert.Equal("Brand2", result.BrandName);
        }

        [Fact]
        public async Task AddNewStationAsyncTest_SuccessIfStationAdded()
        {
            //Arrange
            var newFuelType = new AddFuelTypeAndPriceRequest
            {
                Name = "LPG gas",
                Code = "LPG",
                Price = 5.0m
            };
            var request = new AddStationRequest
            {
                BrandName = "Brand3",
                City = "TestCity3",
                HouseNumber = "3",
                Latitude = 30.0,
                Longitude = 30.0,
                PostalCode = "00-003",
                Street = "TestStreet3",
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    newFuelType
                }
            };

            //Act
            var result = await _repository.AddNewStationAsync(request);

            //Assert
            Assert.True(result);
            Assert.Equal(3, _context.FuelPrices.ToList().Count());
            Assert.Equal(3, _context.Stations.ToList().Count());
            _output.WriteLine("Success, AddNewStationAsync adds a new station.");
        }

        [Fact]
        public async Task AddNewStationAsyncTest_BadFuelType_SuccessIfExcThrown()
        {
            //Arrange
            var badFuelType = new AddFuelTypeAndPriceRequest
            {
                Name = "BAD gas",
                Code = "BAD",
                Price = 5.0m
            };
            var request = new AddStationRequest
            {
                BrandName = "Brand3",
                City = "TestCity3",
                HouseNumber = "3",
                Latitude = 30.0,
                Longitude = 30.0,
                PostalCode = "00-003",
                Street = "TestStreet3",
                FuelTypes = new List<AddFuelTypeAndPriceRequest>
                {
                    badFuelType
                }
            };

            //Act && Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddNewStationAsync(request));
            _output.WriteLine("Success, AddNewStationAsync throws an exception when given a nonexistent fuel type");
        }

        [Fact]
        public async Task DeleteStationAsyncTest_SuccessIfStation2Deleted()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "Brand2",
                City = "TestCity2",
                HouseNumber = "2",
                Street = "TestStreet2"
            };

            //Act
            var result = await _repository.DeleteStationAsync(request);

            //Assert
            Assert.Equal(1, _context.Stations.Count());
            Assert.Equal("Brand1", _context.Stations.ToList().First().Brand.Name);
        }

        [Fact]
        public async Task DeleteStationAsyncTest_BadStation_SuccessIfExcThrown()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "BadBrand",
                City = "BadCity",
                HouseNumber = "BadNumber",
                Street = "BadStreet"
            };

            //Act and Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.DeleteStationAsync(request));
            _output.WriteLine("Success, DeleteStationAsync throws an exception when trying to delete a nonexistent station");
        }

        [Fact]
        public async Task GetPriceProposaByStationAsyncTest_ZeroProposals_SuccessIfReturnsEmpty()
        {
            //Arrange
            var request = new FindStationRequest
            {
                BrandName = "Brand2",
                City = "TestCity2",
                HouseNumber = "2",
                Street = "TestStreet2"
            };

            //Act
            var result = await _repository.GetPriceProposaByStationAsync(request);

            //Assert
            Assert.Empty(result);
            _output.WriteLine("Success, GetPriceProposaByStationAsync returns empty list when station has no pending proposals");
        }

        [Fact]
        public async Task GetPriceProposaByStationAsyncTest_SuccessIfReturnsPendingProposals()
        {
            //Arrange
            var station1 = _context.Stations.First(s => s.Brand.Name == "Brand1");
            var fuel1 = _context.FuelTypes.First(ft => ft.Code == "ON");
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com",
                UserName = "TestUser"
            };
            _context.Users.Add(user);
            _context.PriceProposals.AddRange
                (
                new PriceProposal
                {
                    Id = Guid.NewGuid(),
                    StationId = station1.Id,
                    UserId = user.Id,
                    FuelTypeId = fuel1.Id,
                    ProposedPrice = 100,
                    Status = Data.Enums.PriceProposalStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    PhotoUrl = "url1",
                    Token = Guid.NewGuid().ToString(),
                },
                new PriceProposal
                {
                    Id = Guid.NewGuid(),
                    StationId = station1.Id,
                    UserId = user.Id,
                    FuelTypeId = fuel1.Id,
                    ProposedPrice = 30,
                    Status = Data.Enums.PriceProposalStatus.Accepted,
                    CreatedAt = DateTime.UtcNow,
                    PhotoUrl = "url2",
                    Token = Guid.NewGuid().ToString(),
                },
                new PriceProposal
                {
                    Id = Guid.NewGuid(),
                    StationId = station1.Id,
                    UserId = user.Id,
                    FuelTypeId = fuel1.Id,
                    ProposedPrice = 10,
                    Status = Data.Enums.PriceProposalStatus.Pending,
                    CreatedAt = DateTime.UtcNow,
                    PhotoUrl = "url3",
                    Token = Guid.NewGuid().ToString(),
                }
                );
            await _context.SaveChangesAsync();
            var request = new FindStationRequest
            {
                BrandName = "Brand1",
                City = "TestCity1",
                HouseNumber = "1",
                Street = "TestStreet1"
            };

            //Act
            var result = await _repository.GetPriceProposaByStationAsync(request);

            //Assert
            Assert.Equal(2, result.Count());
        }
    }
}
