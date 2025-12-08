using Data.Context;
using Data.Models;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Services.Helpers;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
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
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task EditBrandAsyncTest_404()
        {
            //Arrange
            var oldName = "BadName";
            var newName = "NewBadName";
            var url = $"/api/admin/brand/edit/{oldName}?newName={newName}";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task EditBrandAsyncTest_400()
        {
            //Arrange
            var oldName = " ";
            var newName = "NewName";
            var url = $"/api/admin/brand/edit/{oldName}?newName={newName}";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddBrandAsyncTest_201()
        {
            //Arrange
            var newBrand = new MultipartFormDataContent
            {
                {new StringContent("NewBrand"), "name" }
            };
            var url = $"/api/admin/brand/add";

            //Act
            var response = await _client.PostAsync(url, newBrand);
            var json = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.Contains("success", json);
            Assert.Contains("true", json);
            Assert.Contains("Brand NewBrand", json);
        }

        [Fact]
        public async Task AddBrandAsyncTest_400()
        {
            //Arrange
            var newBrand = new MultipartFormDataContent
            {
                {new StringContent(""), "name" }
            };
            var url = $"/api/admin/brand/add";

            //Act
            var response = await _client.PostAsync(url, newBrand);
            var json = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task AddBrandAsyncTest_409()
        {
            //Arrange
            var newBrand = new MultipartFormDataContent
            {
                {new StringContent("Orlen"), "name" }
            };
            var url = $"/api/admin/brand/add";

            //Act
            var response = await _client.PostAsync(url, newBrand);

            //Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBrandAsyncTest_200OK()
        {
            //Arrange
            var brandName = "Shell";
            var url = $"/api/admin/brand/{brandName}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBrandAsyncTest_404()
        {
            //Arrange
            var brandName = "BadTest";
            var url = $"/api/admin/brand/{brandName}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteBrandAsyncTest_400()
        {
            //Arrange
            var brandName = "%20";
            var url = $"/api/admin/brand/{brandName}";

            //Act
            var response = await _client.DeleteAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }
    }
}