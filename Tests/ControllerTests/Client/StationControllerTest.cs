using Data.Context;
using DTO.Requests;
using Microsoft.EntityFrameworkCore;
using Services.Helpers;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Tests.ControllerTests;
using Xunit;

namespace Tests.ControllerTest.Client
{
    [Collection("IntegrationTests")]
    public class StationControllerTest : IAsyncLifetime
    {
        private HttpClient _client;
        private CustomAppFact _factory;

        public async Task InitializeAsync()
        {
            _factory = new CustomAppFact();
            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", "test-user-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_200OK()
        {
            //Arrange
            var request = new GetStationsRequest
            {
                BrandName = null,
                LocationLatitude = null,
                LocationLongitude = null
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/station/map/all", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task GetAllStationsForMapAsyncTest_404()
        {
            //Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stations = await db.Stations.ToListAsync();
            if (stations.Any()) db.Stations.RemoveRange(stations);
            await db.SaveChangesAsync();
            var request = new GetStationsRequest
            {
                BrandName = null,
                LocationLatitude = null,
                LocationLongitude = null
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/station/map/all", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_200OK()
        {
            //Arrange
            //--

            //Act
            var response = await _client.GetAsync("api/station/map/nearest?latitude=10&longitude=10&count=3");
            var body = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task GetNearestStationAsyncTest_404()
        {
            //Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var stations = await db.Stations.ToListAsync();
            if (stations.Any()) db.Stations.RemoveRange(stations);
            await db.SaveChangesAsync();

            //Act
            var response = await _client.GetAsync("api/station/map/nearest?latitude=10&longitude=10&count=3");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStationListAsyncTest_200OK()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                LocationLatitude = null,
                LocationLongitude = null,
                Distance = null,
                FuelType = null,
                MinPrice = null,
                MaxPrice = null,
                BrandName = null,
                SortingByDisance = null,
                SortingByPrice = null,
                SortingDirection = null,
                Pagging = new GetPaggedRequest { PageNumber = 1, PageSize = 10 }
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/station/list", content);
            var body = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task GetStationListAsyncTest_400()
        {
            //Arrange
            var request = new GetStationListRequest
            {
                LocationLatitude = null,
                LocationLongitude = null,
                Distance = null,
                FuelType = null,
                MinPrice = null,
                MaxPrice = null,
                BrandName = null,
                SortingByDisance = null,
                SortingByPrice = null,
                SortingDirection = null,
                Pagging = new GetPaggedRequest { PageNumber = 0, PageSize = 0 }
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/station/list", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAllBrandsAsyncTest_200OK()
        {
            //Arrange
            var JsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };
            
            //Act
            var response = await _client.GetAsync("api/station/all-brands");
            var body = await response.Content.ReadAsStringAsync();
            var brands = JsonSerializer.Deserialize<string[]>(body, JsonOptions);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(brands);
            Assert.True(brands.Length > 0);
            Assert.Contains(brands, b => b.ToLower().Contains("orlen") || b.ToLower().Contains("shell"));
        }

        [Fact]
        public async Task GetAllBrandsAsyncTest_404()
        {
            //Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var brands = await db.Brand.ToListAsync();
            if (brands.Any()) db.Brand.RemoveRange(brands);
            await db.SaveChangesAsync();
            var JsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            //Act
            var response = await _client.GetAsync("api/station/all-brands");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetStationProfileAsyncTest_200OK()
        {
            //Arrange
            var url = "api/station/profile?BrandName=Orlen&Street=test&HouseNumber=1&City=test";

            //Act
            var response = await _client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task GetStationProfileAsyncTest_400()
        {
            //Arrange
            var url = "api/station/profile?BrandName=&Street=&HouseNumber=&City=";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetStationProfileAsyncTest_404()
        {
            //Arrange
            var url = "api/station/profile?BrandName=Bad&Street=Bad&HouseNumber=100&City=Bad";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Fact]
        public async Task AddNewPriceProposalAsyncTest_200OK()
        {
            //Arrange
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent("Orlen"), "BrandName");
            content.Add(new StringContent("test"), "Street");
            content.Add(new StringContent("1"), "HouseNumber");
            content.Add(new StringContent("test"), "City");
            content.Add(new StringContent("ON"), "FuelTypeCode");
            var price = 5.50m;
            content.Add(new StringContent(price.ToString(System.Globalization.CultureInfo.CurrentCulture)), "ProposedPrice");
            var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "Photo", "image.jpg");

            //Act
            var response = await _client.PostAsync("api/station/price-proposal/add", content);
            var body = await response.Content.ReadAsStringAsync();

            //Assertr
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task AddNewPriceProposalAsyncTest_400()
        {
            //Arrange
            using var content = new MultipartFormDataContent();
            content.Add(new StringContent(""), "BrandName");
            content.Add(new StringContent(""), "Street");
            content.Add(new StringContent(""), "HouseNumber");
            content.Add(new StringContent(""), "City");
            content.Add(new StringContent(""), "FuelTypeCode");
            var price = 5.50m;
            content.Add(new StringContent(price.ToString(System.Globalization.CultureInfo.CurrentCulture)), "ProposedPrice");
            var imageBytes = new byte[] { 0xFF, 0xD8, 0xFF, 0xD9 };
            var fileContent = new ByteArrayContent(imageBytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
            content.Add(fileContent, "Photo", "image.jpg");

            //Act
            var response = await _client.PostAsync("api/station/price-proposal/add", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetAllFuelTypeCodesAsyncTest_200OK()
        {
            //Arrange
            var JsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            //Act
            var response = await _client.GetAsync("api/station/fuel-codes");
            var body = await response.Content.ReadAsStringAsync();
            var codes = JsonSerializer.Deserialize<string[]>(body, JsonOptions);

            //Assertr
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(codes);
            Assert.True(codes.Length > 0);
            Assert.Contains(codes, c => c == "ON" || c == "LPG" || c == "PB95");
        }

        [Fact]
        public async Task GetAllFuelTypeCodesAsyncTest_404()
        {
            //Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var fuels = await db.FuelTypes.ToListAsync();
            if (fuels.Any()) db.FuelTypes.RemoveRange(fuels);
            await db.SaveChangesAsync();
            var JsonOptions = new JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true
            };

            //Act
            var response = await _client.GetAsync("api/station/fuel-codes");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetPriceProposalByStationAsyncTest_200OK()
        {
            //Arrange
            var url = "api/station/price-proposal?BrandName=Orlen&Street=test&HouseNumber=1&City=test&PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(url);
            var body = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task GetPriceProposalByStationAsyncTest_400K()
        {
            //Arrange
            var url = "api/station/price-proposal?BrandName=&Street=&HouseNumber=&City=&PageNumber=&PageSize=";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}