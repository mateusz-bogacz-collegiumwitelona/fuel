using Data.Context;
using Data.Models;
using DTO.Responses;
using Services.Helpers;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Tests.ControllerTest
{
    public class BrandControllerTest
    {
        private readonly HttpClient _client;
        private readonly CustomAppFact _factory;

        public BrandControllerTest()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-admin-token");
        }

        private void SeedData()
        {
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            if (!db.Brand.Any())
            {
                db.Brand.Add(new Brand
                {
                    Id = Guid.NewGuid(),
                    Name = "Orlen",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                });
                db.SaveChanges();
            }
        }

        [Fact]
        public async Task GetBrandListTest_200OK()
        {
            //Arrange
            var url = "/api/admin/brand/list?Search=Orlen&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<PagedResult<GetBrandDataResponse>>(content, options);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
            Assert.Equal("Orlen", result.Items[0].Name);
        }

        [Fact]
        public async Task GetBrandList_Unauthorized()
        {
            //Arrange
            var url = "/api/admin/brand/list?Search=Orlen&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc";
            _client.DefaultRequestHeaders.Authorization = null;

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EditBrandAsyncTest_200OK()
        {
            //Arrange
            var oldName = "Orlen";
            var newName = "OrlenTest";
            var url = $"/api/admin/brand/edit/{oldName}?newName={newName}";

            //Act
            var response = await _client.PatchAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(content);
            var root = doc.RootElement;
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var brand = db.Brand.FirstOrDefault(b => b.Name == newName);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(brand);
            Assert.Contains(db.Brand, b => b.Name == newName);
            Assert.DoesNotContain(db.Brand, b => b.Name == oldName);
        }

        [Fact]
        public async Task EditBrandAsyncTest_Unauthorized()
        {
            //Arrange
            var oldName = "Orlen";
            var newName = "OrlenTest";
            var url = $"/api/admin/brand/edit/{oldName}?newName={newName}";
            _client.DefaultRequestHeaders.Authorization = null;

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.Unauthorized, response.StatusCode);
        }

        public async Task EditBrandAsyncTest_404()
        {
            //Arrange
            var oldName = "BadName";
            var newName = "NewBadName";
            var url = $"/api/admin/brand/edit/{oldName}?newName={newName}";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
