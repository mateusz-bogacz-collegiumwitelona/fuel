using Data.Context;
using Data.Models;
using Data.Reopsitories;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class BrandRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<BrandRepository>> _loggerMock;
        private readonly BrandRepository _repository;
        private readonly ITestOutputHelper _output;

        public BrandRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<BrandRepository>>();
            var brand1 = new Brand { Id = new Guid(), Name = "Brand1", CreatedAt = new DateTime(2024, 1, 1), UpdatedAt = new DateTime(2025, 5, 1) };
            var brand2 = new Brand { Id = new Guid(), Name = "Brand2", CreatedAt = new DateTime(2024, 2, 1), UpdatedAt = new DateTime(2025, 3, 1) };
            _context.Brand.AddRange(brand1, brand2);
            _context.SaveChanges();
            _repository = new BrandRepository(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_NoFilter_SuccessIfReturnsAll()
        {
            //brak filtrów, pusty request
            //Arrange
            var request = new TableRequest();

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Brand1", "Brand2" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns all brands");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_NameFilterSearch_SuccessIfReturnsBrand2()
        {
            //Arrange
            var request = new TableRequest { Search = "2" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Single(result);
            Assert.All(result, b => Assert.Contains("2", b.Name.ToLower()));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns correct brand after applying a filter");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_FilterByCreatedAtAsc_SuccessIfBrand1OnTop()
        {
            //Arrange
            var request = new TableRequest { SortBy = "createdat", SortDirection = "asc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(new[] { "Brand1", "Brand2" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns correct list filtered by CreatedAt in ascending order");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_FilterByCreatedAtDesc_SuccessIfBrand2OnTop()
        {
            //Arrange
            var request = new TableRequest { SortBy = "createdat", SortDirection = "desc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(new[] { "Brand2", "Brand1" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns correct list filtered by CreatedAt in descsending order");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_FilterByUpdatedAtDesc_SuccessIfBrand1OnTop()
        {
            //Arrange
            var request = new TableRequest { SortBy = "updatedat", SortDirection = "desc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(new[] { "Brand1", "Brand2" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns correct list filtered by UpdatedAt in descending order");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_FilterByUpdatedAtAsc_SuccessIfBrand2OnTop()
        {
            //Arrange
            var request = new TableRequest { SortBy = "updatedat", SortDirection = "asc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(new[] { "Brand2", "Brand1" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns correct list filtered by UpdatedAt in ascending order");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_NameFilterSearchAsc_SuccessIfSortedCorrectly()
        {
            //Arrange
            var request = new TableRequest { SortBy = "name", SortDirection = "asc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Brand1", "Brand2" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns brands in ascending order by name");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_NameFilterSearchDesc_SuccessIfSortedCorrectly()
        {
            //Arrange
            var request = new TableRequest { SortBy = "name", SortDirection = "desc" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Brand2", "Brand1" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns brands in descending order by name");
        }

        [Fact]
        public async Task GetBrandToListAsyncTest_WrongSortBy_SuccessIfSortedByName()
        {
            //Arrange
            var request = new TableRequest { SortBy = "incorrect sortby" };

            //Act
            var result = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(new[] { "Brand1", "Brand2" }, result.Select(r => r.Name));
            _output.WriteLine("Test passed: GetBrandToListAsync() returns brands ordered by name if the SortBy value is incorrect");
        }

        [Fact]
        public async Task EditBrandAsyncTest_AllGood_SuccessIfBrandChanged()
        {
            //Arrange
            string oldName = "Brand1";
            string newName = "NewBrand1";

            //Act
            var result = await _repository.EditBrandAsync(oldName, newName);

            //Assert
            Assert.True(result);
            var UpdatedBrand = await _context.Brand.FirstOrDefaultAsync(b => b.Name == newName);
            Assert.NotNull(UpdatedBrand);
            _output.WriteLine("Test passed: EditBrandAsync() changes name of the brand correctly");
        }

        [Fact]
        public async Task EditBrandAsyncTest_WrongInfo_SuccessIfReturnsFalse()
        {
            //Arrange
            string oldName = "Nonexistant Brand";
            string newName = "Test123";

            //Act
            var result = await _repository.EditBrandAsync(oldName, newName);

            //Assert
            Assert.False(result);
            var UpdatedBrand = await _context.Brand.FirstOrDefaultAsync(b => b.Name == newName);
            Assert.Null(UpdatedBrand);
            _output.WriteLine("Test passed: EditBrandAsync() returns false when you try to edit a non-existent brand");
        }

        [Fact]
        public async Task EditBrandAsyncTest_UpdateTimeChanged_SuccessIfUpdateIsRefreshed()
        {
            //Arrange
            string oldName = "Brand1";
            string newName = "Brand1AAA";

            var brandBeforeUpdate = await _context.Brand.FirstOrDefaultAsync(r => r.Name == oldName);
            var oldUpdateTime = brandBeforeUpdate?.UpdatedAt;

            //Act
            var result = await _repository.EditBrandAsync(oldName, newName);
            var brandAfterUpdate = await _context.Brand.FirstOrDefaultAsync(r => r.Name == newName);

            //Assert
            Assert.True(brandAfterUpdate?.UpdatedAt > oldUpdateTime);
            _output.WriteLine("Test passed: EditBrandAsync() changes the UpdatedAt time");
        }

        [Fact]
        public async Task EditBrandAsyncTest_CaseSensitiveTest_SuccessIfEditsBrandAnyways()
        {
            //Arrange
            string oldName = "brand1";
            string newName = "Brand1AAA";

            //Act
            var result = await _repository.EditBrandAsync(oldName, newName);

            //Assert
            Assert.True(result);
            var updatedBrand = await _context.Brand.FirstOrDefaultAsync(b => b.Name == newName);
            Assert.NotNull(updatedBrand);
            _output.WriteLine("Test passed: EditBrandAsync() is not case sensitive");
        }

        [Fact]
        public async Task AddBrandAsyncTest_AllGood_SuccessIfBrandCreated()
        {
            //Arrange
            string newBrandName = "Brand3";

            //Act
            var result = await _repository.AddBrandAsync(newBrandName);
            var newbrand = await _context.Brand.FirstOrDefaultAsync(r => r.Name == newBrandName);

            //Assert
            Assert.True(result);
            Assert.NotNull(newbrand);
            Assert.Equal(newBrandName, newbrand.Name);
            _output.WriteLine("Test passed: AddBrandAsync() creates a new brand");
        }

        [Fact]
        public async Task AddBrandAsyncTest_AllGood_SuccessIfTimesUpdated()
        {
            //Arrange
            string newBrandName = "Brand3";
            var oldTime = DateTime.UtcNow;

            //Act
            var result = await _repository.AddBrandAsync(newBrandName);
            var newbrand = await _context.Brand.FirstOrDefaultAsync(r => r.Name == newBrandName);
            //Assert
            Assert.True(result);
            Assert.NotNull(newbrand);
            Assert.True(newbrand.CreatedAt >= oldTime);
            Assert.True(newbrand.UpdatedAt >= oldTime);
            _output.WriteLine("Test passed: AddBrandAsync() adds correct created/updated times after creating a brand");
        }

        [Fact]
        public async Task DeleteBrandAsyncTest_BrandDeletion_SuccessIfBrandDeleted()
        {
            //Arrange
            string brandDelete = "Brand1";
            var request = new TableRequest();

            //Act
            var result = await _repository.DeleteBrandAsync(brandDelete);
            var resultCount = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.True(result);
            Assert.Single(resultCount);
            Assert.All(resultCount, b => Assert.Contains("2", b.Name.ToLower()));
            _output.WriteLine("Test passed: DeleteBrandAsync() deletes the correct brand");
        }

        [Fact]
        public async Task DeleteBrandAsyncTest_EmptyBrand_SuccessIfReturnsFalse()
        {
            //Arrange
            string brandDelete = "Nonexistent brand";
            var request = new TableRequest();

            //Act
            var result = await _repository.DeleteBrandAsync(brandDelete);
            var resultCount = await _repository.GetBrandToListAsync(request);

            //Assert
            Assert.False(result);
            Assert.Equal(2, resultCount.Count);
            _output.WriteLine("Test passed: DeleteBrandAsync() returns false when trying to delete a nonexistent brand");
        }
    }
}
