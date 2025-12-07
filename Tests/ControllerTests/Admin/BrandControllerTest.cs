using Data.Context;
using Data.Models;
using DTO.Responses;
using System.Net.Http.Headers;

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
            SeedData();
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
        public async Task GetBrandList_ShouldReturn200()
        {
            var url = "/api/admin/brand/list?Search=Orlen&pageNumber=1&pageSize=10&sortBy=name&sortDirection=asc";

            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadFromJsonAsync<GetBrandDataResponse>();

            Assert.Equal(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
        }
    }
}
