using Data.Context;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class FuelTypeRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<StationRepository>> _loggerMock;
        private readonly FuelTypeRepository _repository;
        private readonly ITestOutputHelper _output;
        public FuelTypeRepositoryTest(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _loggerMock = new Mock<ILogger<StationRepository>>();
            var station1 = Guid.NewGuid();
            var diesel = new FuelType { Id = Guid.NewGuid(), Name = "Diesel", Code = "ON", CreatedAt = DateTime.UtcNow };
            var lpg = new FuelType { Id = Guid.NewGuid(), Name = "Lpg gas", Code = "LPG", CreatedAt = DateTime.UtcNow.AddHours(-1) };
            _context.FuelTypes.AddRange(diesel, lpg);

            _context.SaveChangesAsync();

            _repository = new FuelTypeRepository(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task FindFuelTypeByCodeAsyncTest_SuccessIfReturnsCorrectFuelType()
        {
            //Arrange
            //---

            //Act
            var result = await _repository.FindFuelTypeByCodeAsync("ON");

            //Assert
            Assert.NotNull(result);
            Assert.Equal("ON", result.Code);
            _output.WriteLine("FindFuelTypeByCodeAsync returns the correct fuel type");
        }

        [Fact]
        public async Task FindFuelTypeByCodeAsyncTest_NonexistentFuel_SuccessIfReturnsNull()
        {
            //Arrange
            //---

            //Act
            var result = await _repository.FindFuelTypeByCodeAsync("WrongCode");

            //Assert
            Assert.Null(result);
            _output.WriteLine("FindFuelTypeByCodeAsync returns null if tasked to find a non existent fuel code");
        }

        [Fact]
        public async Task GetAllFuelTypeCodesAsyncTest_SuccessIfAllReturned()
        {
            //Arrange
            //---

            //Act
            var result = await _repository.GetAllFuelTypeCodesAsync();

            //Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Contains("LPG", result);
            Assert.Contains("ON", result);
            _output.WriteLine("GetAllFuelTypeCodesAsync returns all existing fuels");
        }

        [Fact]
        public async Task GetStaionFuelTypesTest_SuccessIfReturnsCorrectPrices()
        {
            //Arrange
            var station1 = Guid.NewGuid();
            var diesel = new FuelType { Id = Guid.NewGuid(), Name = "Diesel", Code = "ON" };
            var lpg = new FuelType { Id = Guid.NewGuid(), Name = "Lpg gas", Code = "LPG" };
            _context.FuelTypes.AddRange(diesel, lpg);
            _context.FuelPrices.AddRange(
                new FuelPrice { StationId = station1, FuelType = diesel, Price = 6 },
                new FuelPrice { StationId = station1, FuelType = lpg, Price = 3 });

            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetStaionFuelTypes(station1);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Contains(result, x => x.Code == "ON" && x.Price == 6);
            Assert.Contains(result, x => x.Code == "LPG" && x.Price == 3);
            _output.WriteLine("GetStaionFuelTypes returns correct prices for fuels");
        }

        [Fact]
        public async Task AddFuelTypeAsyncTest_SuccessIfFuelTypeAdded()
        {
            //Arrange
            //---

            //Act
            var result = await _repository.AddFuelTypeAsync("Petrol 95", "PB95");

            //Assert
            Assert.True(result);
            Assert.Equal(3, _context.FuelTypes.Count());
            _output.WriteLine("AddFuelTypeAsync adds new fuel type correctly");
        }

        [Fact]
        public async Task EditFuelTypeAsyncTest_SuccessIfFuelEdited()
        {
            //Arrange
            var petrol = new FuelType { Id = Guid.NewGuid(), Name = "Petrol 95", Code = "PB95" };
            _context.FuelTypes.Add(petrol);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.EditFuelTypeAsync(petrol, "Petrol 98", "PB98");

            //Assert
            Assert.True(result);
            var updated = _context.FuelTypes.Skip(2).First();
            Assert.Equal(petrol.Id, updated.Id);
            Assert.Equal("Petrol 98", updated.Name);
            Assert.Equal("PB98", updated.Code);
            _output.WriteLine("EditFuelTypeAsync ediits fuel name and code correctly");
        }

        [Fact]
        public async Task DeleteFuelTypeAsyncTest_SuccessIfFuelDeleted()
        {
            //Arrange
            var petrol = new FuelType { Id = Guid.NewGuid(), Name = "Petrol 95", Code = "PB95" };
            _context.FuelTypes.Add(petrol);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.DeleteFuelTypeAsync(petrol);
            var list = await _repository.GetAllFuelTypeCodesAsync();

            //Assert
            Assert.True(result);
            Assert.Equal(2, _context.FuelTypes.Count());
            Assert.DoesNotContain("PB95", list);
            Assert.Contains("LPG", list);
            Assert.Contains("ON", list);
            _output.WriteLine("Success, DeleteFuelTypeAsync deletes a fuel type");
        }

        [Fact]
        public async Task GetFuelsTypeListAsyncTest_SuccessIfALlReturned()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = null,
                SortBy = null,
                SortDirection = null,
            };

            //Act
            var result = await _repository.GetFuelsTypeListAsync(request);

            //Assert
            Assert.Equal(2, result.Count);
            _output.WriteLine("Success, GetFuelsTypeListAsync returns all fuels with no filter");
        }

        [Fact]
        public async Task GetFuelsTypeListAsyncTest_SearchFilter_SuccessIfLPGReturned()
        {
            //Arrange
            var request = new TableRequest
            {
                Search = "lpg",
                SortBy = null,
                SortDirection = null,
            };

            //Act
            var result = await _repository.GetFuelsTypeListAsync(request);

            //Assert
            Assert.Single(result);
            Assert.Equal("LPG", result.First().Code);
            _output.WriteLine("Success, GetFuelsTypeListAsync searches for the correct fuel");
        }

        [Fact]
        public async Task GetFuelsTypeListAsyncTest_SuccessIfSortedAscDesc()
        {
            //Arrange
            var requestAsc = new TableRequest
            {
                Search = null,
                SortBy = "name",
                SortDirection = "asc"
            };

            var requestDesc = new TableRequest
            {
                Search = null,
                SortBy = "name",
                SortDirection = "desc"
            };

            //Act
            var resultAsc = await _repository.GetFuelsTypeListAsync(requestAsc);
            var resultDesc = await _repository.GetFuelsTypeListAsync(requestDesc);

            //Assert
            Assert.Equal("Diesel", resultAsc[0].Name);
            Assert.Equal("Lpg gas", resultAsc[1].Name);
            Assert.Equal("Diesel", resultDesc[1].Name);
            Assert.Equal("Lpg gas", resultDesc[0].Name);
            _output.WriteLine("Success, GetFuelsTypeListAsync sorts correctly.");
        }

        [Fact]

        public async Task GetFuelsTypeListAsyncTest_CreatedBySort_SuccessIfSortedByDate()
        {
            //Arrange
            var requestAsc = new TableRequest
            {
                Search = null,
                SortBy = "createdat",
                SortDirection = "asc"
            };
            var requestDesc = new TableRequest
            {
                Search = null,
                SortBy = "createdat",
                SortDirection = "desc"
            };

            //Act
            var resultAsc = await _repository.GetFuelsTypeListAsync(requestAsc);
            var resultDesc = await _repository.GetFuelsTypeListAsync(requestDesc);

            //Assert
            Assert.Equal("Diesel", resultAsc[1].Name);
            Assert.Equal("Lpg gas", resultAsc[0].Name);
            Assert.Equal("Diesel", resultDesc[0].Name);
            Assert.Equal("Lpg gas", resultDesc[1].Name);
            _output.WriteLine("Success, GetFuelsTypeListAsync sorts correctly.");
        }

        [Fact]
        public async Task AssignFuelTypeToStationAsyncTest_SuccessIfAssigned()
        {
            //Arrange
            var station = new Station
            {
                Id = Guid.NewGuid()
            };
            _context.Stations.Add(station);
            var petrol = new FuelType { Id = Guid.NewGuid(), Name = "Petrol 95", Code = "PB95" };
            _context.FuelTypes.Add(petrol);

            //Act
            var result = await _repository.AssignFuelTypeToStationAsync(petrol.Id, station.Id, 5.50m);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(1, _context.FuelPrices.ToList().Count());
            Assert.Equal(station.Id, _context.FuelPrices.ToList().First().StationId);
            Assert.Equal(petrol.Name, _context.FuelPrices.ToList().First().FuelType.Name);
            _output.WriteLine("Success, AssignFuelTypeToStationAsync assigns fuel type to a station.");
        }

        [Fact]
        public async Task AssignFuelTypeToStationAsyncTest_AlreadyAssigned_SuccessIfReturnsFalse()
        {
            //Arrange
            var station = new Station
            {
                Id = Guid.NewGuid()
            };
            _context.Stations.Add(station);
            var petrol = new FuelType { Id = Guid.NewGuid(), Name = "Petrol 95", Code = "PB95" };
            _context.FuelTypes.Add(petrol);
            var fuelprice = new FuelPrice
            {
                FuelTypeId = petrol.Id,
                StationId = station.Id,
                Price = 4.5m
            };
            _context.FuelPrices.Add(fuelprice);
            _context.SaveChangesAsync();

            //Act
            var result = await _repository.AssignFuelTypeToStationAsync(petrol.Id, station.Id, 3.0m);

            //Assert
            Assert.False(result);
            Assert.NotEqual(3.0m, _context.FuelPrices.First().Price);
            _output.WriteLine("Success, AssignFuelTypeToStationAsync doesn't assign the fuelprice if the station/fuel combo exists already");
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_SuccessIfPriceGet()
        {
            //Arrange
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var address = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "testStreet",
                City = "testCity",
                HouseNumber = "1",
                Location = geometryFactory.CreatePoint(new Coordinate(10.0, 10.0))
            };
            var brand = new Brand
            {
                Id = Guid.NewGuid(),
                Name = "TestBrand",
            };
            var station = new Station
            {
                Id = Guid.NewGuid(),
                Address = address,
                AddressId = address.Id,
                BrandId = brand.Id,
                Brand = brand
            };
            var petrol = new FuelType { Id = Guid.NewGuid(), Name = "Petrol 95", Code = "PB95" };
            var fuelprice = new FuelPrice
            {
                StationId = station.Id,
                FuelTypeId = petrol.Id,
                FuelType = petrol,
                Price = 4.5m
            };
            _context.StationAddress.Add(address);
            _context.Brand.Add(brand);
            _context.Stations.Add(station);
            _context.FuelTypes.Add(petrol);
            _context.FuelPrices.Add(fuelprice);
            await _context.SaveChangesAsync();

            var request = new FindStationRequest
            {
                BrandName = brand.Name,
                Street = address.Street,
                City = address.City,
                HouseNumber = address.HouseNumber
            };

            //Act
            var result = await _repository.GetFuelPriceForStationAsync(request);

            //Assert
            Assert.Single(result);
            Assert.Contains(result, fc => fc.FuelCode == "PB95");
            _output.WriteLine("Success, GetFuelPriceForStationAsync finds ");
        }

        [Fact]
        public async Task GetFuelPriceForStationAsyncTest_BadRequest_SuccessIfEmptyListReturned()
        {
            //Arrange
            var request = new FindStationRequest
            {
                Street = "Bad",
                City = "Bad",
                BrandName = "Bad",
                HouseNumber = "Bad"
            };

            //Act
            var result = await _repository.GetFuelPriceForStationAsync(request);

            //Assert
            Assert.Empty(result);
            _output.WriteLine("Success, GetFuelPriceForStationAsync returns an empty list when a bad request is made");
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_BadData_SuccessIfReturnsFalse()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.ChangeFuelPriceAsync(Guid.NewGuid(), Guid.NewGuid(), 100.0m);

            //Assert
            Assert.False(result);
            _output.WriteLine("Success, ChangeFuelPriceAsync returns false when given bad data");
        }

        [Fact]
        public async Task ChangeFuelPriceAsyncTest_SuccessIfChanged()
        {
            //Arrange
            var fuelPrice = new FuelPrice
            {
                StationId = Guid.NewGuid(),
                FuelTypeId = Guid.NewGuid(),
                Price = 5.0m
            };
            _context.FuelPrices.Add(fuelPrice);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.ChangeFuelPriceAsync(fuelPrice.StationId, fuelPrice.FuelTypeId, 100.0m);

            //Assert
            Assert.Equal(100.0m, _context.FuelPrices.First().Price);
            Assert.Equal(fuelPrice.Id, _context.FuelPrices.First().Id);
            _output.WriteLine("Sucecss, ChangeFuelPriceAsync changes the price correctly.");
        }
    }
}
