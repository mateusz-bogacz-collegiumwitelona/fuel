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
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class BrandServicesTest
    {
        private readonly Mock<Data.Interfaces.IBrandRepository> _brandRepoMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly CacheService _cache;
        private readonly Mock<ILogger<BrandServices>> _loggerMock;
        private readonly BrandServices _service;
        private readonly ITestOutputHelper _output;

        public BrandServicesTest(ITestOutputHelper output)
        {
            _output = output;
            _brandRepoMock = new Mock<Data.Interfaces.IBrandRepository>();
            _loggerMock = new Mock<ILogger<BrandServices>>();
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);
            _cache = new CacheService(_redisMock.Object, Mock.Of<ILogger<CacheService>>());
            _loggerMock = new Mock<ILogger<BrandServices>>();
            _service = new BrandServices(
                _brandRepoMock.Object,
                _loggerMock.Object,
                _cache
            );
        }

        [Fact]
        public async Task GetAllBrandsAsync_ReturnsNotFound_WhenCacheAndRepoEmpty()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.GetAllBrandsAsync()).ReturnsAsync(new List<string>());

            // Act
            var result = await _service.GetAllBrandsAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.NotNull(result.Errors);
            _output.WriteLine("Test passed: GetAllBrandsAsync returns 404 when no brands available");
        }

        [Fact]
        public async Task GetAllBrandsAsync_ReturnsList_WhenCacheHasValues()
        {
            // Arrange
            var brands = new List<string> { "Brand1", "Brand2" };
            
            _brandRepoMock.Setup(r => r.GetAllBrandsAsync()).ReturnsAsync(brands);

            // Act
            var result = await _service.GetAllBrandsAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Equal(2, result.Data?.Count);
            _output.WriteLine("Test passed: GetAllBrandsAsync returns list (from factory/cache)");
        }

        [Fact]
        public async Task EditBrandAsync_ReturnsBadRequest_WhenOldNameInvalid()
        {
            // Act
            var result = await _service.EditBrandAsync("", "NewName");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: EditBrandAsync validates old name");
        }

        [Fact]
        public async Task EditBrandAsync_ReturnsBadRequest_WhenNewNameInvalid()
        {
            // Act
            var result = await _service.EditBrandAsync("OldName", "   ");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: EditBrandAsync validates new name");
        }

        [Fact]
        public async Task EditBrandAsync_ReturnsNotFound_WhenOldDoesNotExist()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("Old")).ReturnsAsync(false);

            // Act
            var result = await _service.EditBrandAsync("Old", "New");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: EditBrandAsync returns 404 when old brand missing");
        }

        [Fact]
        public async Task EditBrandAsync_ReturnsConflict_WhenNewAlreadyExists()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("Old")).ReturnsAsync(true);
            _brandRepoMock.Setup(r => r.FindBrandAsync("New")).ReturnsAsync(true);

            // Act
            var result = await _service.EditBrandAsync("Old", "New");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
            _output.WriteLine("Test passed: EditBrandAsync returns conflict when new brand already exists");
        }

        [Fact]
        public async Task EditBrandAsync_Successful_EditAndInvalidateCache()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("Old")).ReturnsAsync(true);
            _brandRepoMock.Setup(r => r.FindBrandAsync("New")).ReturnsAsync(false);
            _brandRepoMock.Setup(r => r.EditBrandAsync("Old", "New")).ReturnsAsync(true);

            // Act
            var result = await _service.EditBrandAsync("Old", "New");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.True(_dbMock.Invocations.Any(inv => inv.Method.Name.IndexOf("KeyDelete", StringComparison.OrdinalIgnoreCase) >= 0),
                "Expected cache key delete to be invoked during cache invalidation.");

            _output.WriteLine("Test passed: EditBrandAsync edits brand and triggers cache invalidation");
        }

        [Fact]
        public async Task AddBrandAsync_ReturnsBadRequest_WhenNameInvalid()
        {
            // Act
            var result = await _service.AddBrandAsync(" ");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: AddBrandAsync validates name");
        }

        [Fact]
        public async Task AddBrandAsync_ReturnsConflict_WhenBrandExists()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("BrandX")).ReturnsAsync(true);

            // Act
            var result = await _service.AddBrandAsync("BrandX");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
            _output.WriteLine("Test passed: AddBrandAsync returns conflict when brand exists");
        }

        [Fact]
        public async Task AddBrandAsync_Successful_AddAndInvalidateCache()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("BrandNew")).ReturnsAsync(false);
            _brandRepoMock.Setup(r => r.AddBrandAsync("BrandNew")).ReturnsAsync(true);

            // Act
            var result = await _service.AddBrandAsync("BrandNew");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);

            Assert.True(_dbMock.Invocations.Any(inv => inv.Method.Name.IndexOf("KeyDelete", StringComparison.OrdinalIgnoreCase) >= 0),
                "Expected cache key delete to be invoked during cache invalidation.");

            _output.WriteLine("Test passed: AddBrandAsync adds brand and triggers cache invalidation");
        }

        [Fact]
        public async Task DeleteBrandAsync_ReturnsBadRequest_WhenNameInvalid()
        {
            // Act
            var result = await _service.DeleteBrandAsync("");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: DeleteBrandAsync validates name");
        }

        [Fact]
        public async Task DeleteBrandAsync_ReturnsNotFound_WhenBrandMissing()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("X")).ReturnsAsync(false);

            // Act
            var result = await _service.DeleteBrandAsync("X");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: DeleteBrandAsync returns 404 when brand missing");
        }

        [Fact]
        public async Task DeleteBrandAsync_Successful_DeleteAndInvalidateCache()
        {
            // Arrange
            _brandRepoMock.Setup(r => r.FindBrandAsync("ToDelete")).ReturnsAsync(true);
            _brandRepoMock.Setup(r => r.DeleteBrandAsync("ToDelete")).ReturnsAsync(true);

            // Act
            var result = await _service.DeleteBrandAsync("ToDelete");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);

            Assert.True(_dbMock.Invocations.Any(inv => inv.Method.Name.IndexOf("KeyDelete", StringComparison.OrdinalIgnoreCase) >= 0),
                "Expected cache key delete to be invoked during cache invalidation.");

            _output.WriteLine("Test passed: DeleteBrandAsync deletes brand and triggers cache invalidation");
        }
    }
}

