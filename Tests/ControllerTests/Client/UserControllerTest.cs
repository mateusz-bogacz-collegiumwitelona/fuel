using contlollers.Controllers.Client;
using Data.Context;
using DTO.Requests;
using Services.Interfaces;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Tests.ControllerTests;

namespace Tests.ControllerTest.Client
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
                new AuthenticationHeaderValue("Bearer", "test-user-token");
            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            _client?.Dispose();
            await _factory.DisposeAsync();
        }

        [Fact]
        public async Task ChangeUserNameAsyncTest_200OK()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PatchAsync("api/user/change-name?userName=NewUserName", null);
            var body = await response.Content.ReadAsStringAsync();

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.False(string.IsNullOrWhiteSpace(body));
        }

        [Fact]
        public async Task ChangeUserNameAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.PatchAsync("api/user/change-name?userName=NewUserName", null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserEmailAsyncTest_200OK()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PatchAsync("api/user/change-email?newEmail=newemail@test.com", null);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
        }

        [Fact]
        public async Task ChangeUserEmailAsyncTest_400()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PatchAsync("api/user/change-email?newEmail=.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserEmailAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.PatchAsync("api/user/change-email?newEmail=new@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserEmailAsyncTest_404()
        {
            //Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userMail = await db.Users
                        .SingleOrDefaultAsync(ur => ur.UserName == user.UserName && ur.Id == user.Id);
                    if (userMail != null)
                    {
                        db.Users.Remove(userMail);
                        await db.SaveChangesAsync();
                    }
                }
            }

            //Act
            var response = await _client.PatchAsync("api/user/change-email?newEmail=new@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserEmailAsyncTest_409()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PatchAsync("api/user/change-email?newEmail=user2@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserPasswordAsyncTest_200OK()
        {
            //Arrange
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "User123!",
                NewPassword = "NewPass1!",
                ConfirmNewPassword = "NewPass1!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/user/change-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
        }

        [Fact]
        public async Task ChangeUserPasswordAsyncTest_400()
        {
            //Arrange
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "User123!",
                NewPassword = "NewPass1!",
                ConfirmNewPassword = "BadPass123!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/user/change-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserPasswordAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "User123!",
                NewPassword = "NewPass1!",
                ConfirmNewPassword = "NewPass1!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PatchAsync("/api/user/change-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ChangeUserPassowrdAsyncTest_404()
        {
            //Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userMail = await db.Users
                        .SingleOrDefaultAsync(ur => ur.UserName == user.UserName && ur.Id == user.Id);
                    if (userMail != null)
                    {
                        db.Users.Remove(userMail);
                        await db.SaveChangesAsync();
                    }
                }
            }
            var request = new ChangePasswordRequest
            {
                CurrentPassword = "User123!",
                NewPassword = "NewPass1!",
                ConfirmNewPassword = "NewPass1!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PatchAsync("/api/user/change-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserAsyncTest_200OK()
        {
            //Arrange
            var request = new DeleteAccountRequest
            {
                Password = "User123!",
                ConfirmPassword = "User123!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete") { Content = content };
            var response = await _client.SendAsync(req);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
        }

        [Fact]
        public async Task DeleteUserAsyncTest_400()
        {
            //Arrange
            var request = new DeleteAccountRequest
            {
                Password = "User123!",
                ConfirmPassword = "BadPassword!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete") { Content = content };
            var response = await _client.SendAsync(req);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var request = new DeleteAccountRequest
            {
                Password = "User123!",
                ConfirmPassword = "BadPassword!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete") { Content = content };
            var response = await unauthClient.SendAsync(req);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task DeleteUserAsyncTest_404()
        {
            //Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userMail = await db.Users
                        .SingleOrDefaultAsync(ur => ur.UserName == user.UserName && ur.Id == user.Id);
                    if (userMail != null)
                    {
                        db.Users.Remove(userMail);
                        await db.SaveChangesAsync();
                    }
                }
            }
            var request = new DeleteAccountRequest
            {
                Password = "User123!",
                ConfirmPassword = "User123!"
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            using var req = new HttpRequestMessage(HttpMethod.Delete, "/api/user/delete") { Content = content };
            var response = await _client.SendAsync(req);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ReportUserAsyncTest_200OK()
        {
            //Arrange
            var longReason = new string('a', 60);
            var request = new ReportRequest
            {
                ReportedUserName = "USER2",
                Reason = longReason
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/user/report", content);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(root.TryGetProperty("data", out var data) && data.ValueKind == JsonValueKind.True);
        }

        [Fact]
        public async Task ReportUserAsyncTest_400()
        {
            //Arrange
            var longReason = new string('a', 60);
            var request = new ReportRequest
            {
                ReportedUserName = "",
                Reason = longReason
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/user/report", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ReportUserAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();
            var longReason = new string('a', 60);
            var request = new ReportRequest
            {
                ReportedUserName = "",
                Reason = longReason
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await unauthClient.PostAsync("/api/user/report", content);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task ReportUserAsyncTest_404()
        {
            //Arrang
            var longReason = new string('a', 60);
            var request = new ReportRequest
            {
                ReportedUserName = "IDontExist",
                Reason = longReason
            };
            using var content = new StringContent(JsonSerializer.Serialize(request), Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("/api/user/report", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

    }
}