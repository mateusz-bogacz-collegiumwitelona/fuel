using Data.Context;
using Data.Models;
using Data.Reopsitories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
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
            var diesel = new FuelType { Id = Guid.NewGuid(), Name = "Diesel", Code = "ON" };
            var lpg = new FuelType { Id = Guid.NewGuid(), Name = "Lpg gas", Code = "LPG" };
            _context.FuelTypes.AddRange(diesel, lpg);

            _context.FuelPrices.AddRange(
                new FuelPrice { StationId = station1, FuelType = diesel, Price = 10 },
                new FuelPrice { StationId = station1, FuelType = lpg, Price = 15 });

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
        }
    }
}
