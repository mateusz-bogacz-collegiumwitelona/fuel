using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
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

        [Fact]
        public async Task LockoutUserAsyncTest_200OK()
        {
            //Arrange
            var request = new SetLockoutForUserRequest
            {
                Email = "user@test.com",
                Days = 1,
                Reason = "Test reason, 1 day ban"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/user/lock-out", content);
            var result = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("banned successfully", result);
        }

        [Fact]
        public async Task LockoutUserAsyncTest_400()
        {
            //Arrange
            var request = new SetLockoutForUserRequest
            { 

            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/user/lock-out", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task LockoutUserAsyncTest_401()
        {
            //Arrange
            _client.DefaultRequestHeaders.Authorization = null;
            var request = new SetLockoutForUserRequest
            {
                Email = "user@test.com",
                Days = 1,
                Reason = "Test reason, 1 day ban"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/user/lock-out", content);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LockoutUserAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var request = new SetLockoutForUserRequest
            {
                Email = "user@test.com",
                Days = 1,
                Reason = "Test reason, 1 day ban"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PostAsync("/api/admin/user/lock-out", content);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LockoutUserAsyncTest_404()
        {
            //Arrange
            var request = new SetLockoutForUserRequest
            {
                Email = "bad@user.com",
                Days = 1,
                Reason = "Test reason, 1 day ban"
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/admin/user/lock-out", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReviewLockoutAsyncTest_200OK()
        {
            //Arrange
            var lockoutRequest = new SetLockoutForUserRequest
            {
                Email = "user@test.com",
                Days = 100,
                Reason = "Test ban123123"
            };
            var lockoutContent = new StringContent(JsonSerializer.Serialize(lockoutRequest), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/admin/user/lock-out", lockoutContent);
            await Task.Delay(100);
            //Act
            var response = await _client.GetAsync("/api/admin/user/lock-out/review?email=user@test.com");
            var responseContent = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            using var jsonDoc = JsonDocument.Parse(responseContent);
            var root = jsonDoc.RootElement;
            var dataElement = root.GetProperty("data");
            var result = JsonSerializer.Deserialize<ReviewUserBanResponses>(dataElement.GetRawText(), options);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result.UserName);
            Assert.Equal("TestUser", result.UserName);
            Assert.NotNull(result.Reason);
            Assert.Equal("Test ban123123", result.Reason);
            Assert.NotEqual(default, result.BannedAt);
            Assert.NotNull(result.BannedUntil);
            Assert.NotEqual(DateTime.MaxValue, result.BannedUntil);
            Assert.NotNull(result.BannedBy);
            Assert.Equal("TestAdmin", result.BannedBy);
        }

        [Fact]
        public async Task ReviewLockoutAsyncTest_400()
        {
            //Act
            var response = await _client.GetAsync("/api/admin/user/lock-out/review?email=");

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReviewLockoutAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");

            //Act
            var response = await unauthClient.GetAsync("/api/admin/user/lock-out/review?email=");

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ReviewLockoutAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.GetAsync("/api/admin/user/lock-out/review?email=");

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ReviewLockoutAsyncTest_404_()
        {
            //Act
            var response = await _client.GetAsync("/api/admin/user/lock-out/review?email=bad@test.com");

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task UnlockUserAsyncTest_200OK()
        {
            //Arrange
            var lockoutRequest = new SetLockoutForUserRequest
            {
                Email = "user@test.com",
                Days = 7,
                Reason = "Test ban for unlock"
            };
            var lockoutContent = new StringContent(JsonSerializer.Serialize(lockoutRequest), Encoding.UTF8, "application/json");
            await _client.PostAsync("/api/admin/user/lock-out", lockoutContent);
            await Task.Delay(100);

            //Act
            var response = await _client.PostAsync("/api/admin/user/unlock?userEmail=user@test.com", null);
            var content = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("unlocked successfully", content);
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
            var user = await userManager.FindByEmailAsync("user@test.com");
            Assert.NotNull(user);
            Assert.Null(user.LockoutEnd);
        }

        [Fact]
        public async Task UnlockUserAsyncTest_400()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PostAsync("/api/admin/user/unlock?userEmail=", null);
            
            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task UnlockUserAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.PostAsync("/api/admin/user/unlock?userEmail=user@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task UnlockUserAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");

            //Act
            var response = await unauthClient.PostAsync("/api/admin/user/unlock?userEmail=user@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task UnlockUserAsyncTest_404()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PostAsync("/api/admin/user/unlock?userEmail=bad@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task GetUserReportAsyncTest_200OK()
        {

            //Arrange
            var url = "/api/admin/user/report/list?email=user@test.com&PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(url);
            var content = await response.Content.ReadAsStringAsync();
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            using var jsonDoc = JsonDocument.Parse(content);
            var root = jsonDoc.RootElement;
            var dataElement = root.GetProperty("data");
            var result = JsonSerializer.Deserialize<PagedResult<UserReportsResponse>>(dataElement.GetRawText(), options);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(result);
            Assert.NotNull(result.Items);
        }

        [Fact]
        public async Task GetUserReportAsyncTest_400()
        {
            //Arrange
            var url = "/api/admin/user/report/list?email=&PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GetUserReportAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var url = "/api/admin/user/report/list?email=bad@test.com&PageNumber=1&PageSize=10";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetUserReportAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var url = "/api/admin/user/report/list?email=bad@test.com&PageNumber=1&PageSize=10";

            //Act
            var response = await unauthClient.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetUserReportAsyncTest_404()
        {
            //Arrange
            var url = "/api/admin/user/report/list?email=bad@test.com&PageNumber=1&PageSize=10";

            //Act
            var response = await _client.GetAsync(url);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeReportStatusAsyncTest_200OK()
        {
            //Arrange
            var request = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = "user@test.com",
                ReportingUserEmail = "admin@test.com",
                ReportCreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                Reason = "Test report reason",
                Days = 10
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/admin/user/report/change-status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Contains("success", responseContent);
        }

        [Fact]
        public async Task ChangeReportStatusAsyncTest_400()
        {
            //Arrange
            var request = new ChangeReportStatusRequest
            {
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/admin/user/report/change-status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeReportStatusAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var request = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = "user@test.com",
                ReportingUserEmail = "admin@test.com",
                ReportCreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                Reason = "Test report reason",
                Days = 10
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PatchAsync("/api/admin/user/report/change-status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeReportStatusAsyncTest_403()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            unauthClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-user-token");
            var request = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = "user@test.com",
                ReportingUserEmail = "admin@test.com",
                ReportCreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                Reason = "Test report reason",
                Days = 10
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PatchAsync("/api/admin/user/report/change-status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task ChangeReportStatusAsyncTest_404()
        {
            //Arrange
            var request = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = "bad@test.com",
                ReportingUserEmail = "admin@test.com",
                ReportCreatedAt = new DateTime(2025, 12, 1, 10, 0, 0, DateTimeKind.Utc),
                Reason = "Test report reason",
                Days = 10
            };
            var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/admin/user/report/change-status", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}

