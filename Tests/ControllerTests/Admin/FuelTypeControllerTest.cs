using Data.Context;
using DTO.Requests;
using DTO.Responses;
using Services.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Tests.ControllerTests;

namespace Tests.ControllerTest.Admin
{
    [Collection("IntegrationTests")]
    public class FuelTypeControllerTest : IAsyncLifetime
    {
        private HttpClient _client;
        private CustomAppFact _factory;

        public async Task InitializeAsync()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-admin-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }


        [Fact]
        public async Task GetFuelsTypeListTest_Unauthorized()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var url = "/api/admin/fuel-type/list?pageNumber=1&amp;pageSize=10&amp;search=PB&amp;sortBy=name&amp;sortDirection=asc";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelsTypeListTest_400()
        {
            //Arrange
            var url = "/api/admin/fuel-type/list?PageNumber=bad";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetFuelsTypeListTest_200OK()
        {
            //Arrange
            var url = "/api/admin/fuel-type/list?pageNumber=1&amp;pageSize=10&amp;search=PB&amp;sortBy=name&amp;sortDirection=asc";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<PagedResult<GetBrandDataResponse>>(content, options);
            Assert.NotNull(content);
            Assert.Equal("Diesel", result.Items[0].Name);
        }

        [Fact]
        public async Task AddFuelTypeAsyncTest_201()
        {
            //Arrange
            var request = new AddFuelTypeRequest
            {
                Code = "TEST",
                Name = "Test fuel"
            };
            var url = "/api/admin/fuel-type/add";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json"
);          
            //Act
            var response = await _client.PostAsync(url, json);
 
            //Assert
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fuel = await dbContext.FuelTypes.FirstOrDefaultAsync(f => f.Code == "TEST");
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Equal(request.Code, fuel.Code);
        }

        [Fact]
        public async Task AddFuelTypeAsyncTest_401()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new AddFuelTypeRequest
            {
                Code = "TEST",
                Name = "Test fuel"
            };
            var url = "/api/admin/fuel-type/add";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task AddFuelTypeAsyncTest_409()
        {
            //Arrange
            var request = new AddFuelTypeRequest
            {
                Code = "ON",
                Name = "Test fuel"
            };
            var url = "/api/admin/fuel-type/add";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task AddFuelTypeAsyncTest_400()
        {
            //Arrange
            var request = new AddFuelTypeRequest
            {
                Code = "  ",
                Name = "Test fuel"
            };
            var url = "/api/admin/fuel-type/add";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task EditFuelTypeAsyncTest_200OK()
        {
            //Arrange
            var request = new EditFuelTypeRequest
            {
                OldCode = "PB98",
                NewCode = "PB100",
                NewName = "ZBenzyna jakaś dziwna"
            };
            var url = "/api/admin/fuel-type/edit";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync(url, json);
            
            //Assert
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fuel = await dbContext.FuelTypes.FirstOrDefaultAsync(f => f.Code == "PB100");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(request.NewCode, fuel.Code);
        }

        [Fact]
        public async Task EditFuelTypeAsyncTest_404()
        {
            var request = new EditFuelTypeRequest
            {
                OldCode = "BadCode",
                NewCode = "ONT",
                NewName = "DieselT"
            };
            var url = "/api/admin/fuel-type/edit";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");
            
            //Act
            var response = await _client.PatchAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task EditFuelTypeAsyncTest_400()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new EditFuelTypeRequest
            {
                OldCode = "BadCode",
                NewCode = "ONT",
                NewName = "DieselT"
            };
            var url = "/api/admin/fuel-type/edit";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }
        [Fact]
        public async Task EditFuelTypeAsyncTest_409()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new EditFuelTypeRequest
            {
                OldCode = "BadCode",
                NewCode = "LPG",
                NewName = "DieselT"
            };
            var url = "/api/admin/fuel-type/edit";
            var json = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync(url, json);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFuelTypeAsyncTest_200OK()
        {
            //Arrange
            var code = "X";
            var url = $"/api/admin/fuel-type/delete?CODE={code}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var scope = _factory.Services.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fuel = await dbContext.FuelTypes.FirstOrDefaultAsync(f => f.Code == "X");
            Assert.Null(null);
        }

        [Fact]
        public async Task DeleteFuelTypeAsyncTest_404()
        {
            //Arrange
            var code = "BAD";
            var url = $"/api/admin/fuel-type/delete?CODE={code}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFuelTypeAsyncTest_401()
        {
            //Arrange
            var code = "X";
            var url = $"/api/admin/fuel-type/delete?CODE={code}";
            _client.DefaultRequestHeaders.Authorization = null;

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteFuelTypeAsyncTest_400()
        {
            //Arrange
            var code = "  ";
            var url = $"/api/admin/fuel-type/delete?CODE={code}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

    }
}
