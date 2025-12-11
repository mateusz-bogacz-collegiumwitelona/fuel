using Data.Context;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Services.Helpers;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Tests.ControllerTest.Admin
{
    [Collection("IntegrationTests")]
    public class UserControllerTest : IAsyncLifetime
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
        public async Task GetUserListAsyncTest_200OK()
        {
            //Arrange
            var url = "/api/admin/user/list?page=1&pageSize=10&search=test&sortBy=username";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            var result = JsonSerializer.Deserialize<PagedResult<GetUserListResponse>>(content, options);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
            Assert.Contains(result.Items, item => item.UserName == "TestAdmin");
        }

        [Fact]
        public async Task GetUserListAsyncTest_400()
        {
            //Arrange
            var url = "/api/admin/user/list?page=BadPage&pageSize=BadSize&search=BadSearch&sortBy=BadSort";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetUserListAsyncTest_401()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var url = "/api/admin/user/list?page=1&pageSize=10&search=test&sortBy=username";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserListAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/user/list?page=1&pageSize=10&search=test&sortBy=username";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsyncTest_200OK()
        {
            //Arrange
            var url = "/api/admin/user/change-role?email=user@test.com&newRole=Admin";

            // Act
            var response = await _client.PatchAsync(url, null);
            var content = await response.Content.ReadAsStringAsync();

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(content);
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
            var user = await userManager.FindByEmailAsync("user@test.com");
            Assert.NotNull(user);
            var roles = await userManager.GetRolesAsync(user);
            Assert.Contains("Admin", roles);
        }

        [Fact]
        public async Task ChangeUserRoleAsyncTest_400()
        {
            //Arrange
            var url = "/api/admin/user/change-role?eee";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsyncTest_401()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var url = "/api/admin/user/change-role?email=user@test.com&newRole=Admin";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/user/change-role?email=user@test.com&newRole=Admin";

            //Act
            var response = await unauthClient.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsyncTest_404()
        {
            //Arrange
            var url = "/api/admin/user/change-role?email=BADUSER@test.com&newRole=Admin";

            //Act
            var response = await _client.PatchAsync(url, null);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

