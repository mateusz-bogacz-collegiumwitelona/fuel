using Data.Context;
using DTO.Requests;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Tests.ControllerTests;
using Xunit;

namespace Tests.ControllerTest.Client
{
    [Collection("IntegrationTests")]
    public class LoginRegisterContlollerTest : IAsyncLifetime
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

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        private async Task<HttpResponseMessage> PostJsonAsync(string url, object payload)
        {
            var json = JsonSerializer.Serialize(payload);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            return await _client.PostAsync(url, content);
        }

        [Fact]
        public async Task LoginAsyncTest_200OK()
        {
            var request = new LoginRequest
            {
                Email = "user@test.com",
                Password = "User123!"
            };

            var resp = await PostJsonAsync("api/login", request);

            Assert.Equal(HttpStatusCode.OK, resp.StatusCode);

            var body = await resp.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);
            Assert.NotNull(data);
            Assert.False(string.IsNullOrWhiteSpace(data.Email));
            Assert.False(string.IsNullOrWhiteSpace(data.UserName));
            Assert.NotNull(data.Roles);
        }

        [Fact]
        public async Task LoginAsyncTest_401()
        {
            //Arrange
            var request = new LoginRequest
            {
                Email = "user@test.com",
                Password = "BadPassword123!"
            };

            //Act
            var response = await PostJsonAsync("api/login", request);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task LoginAsyncTest_403()
        {
            //Arrange
            var request = new LoginRequest
            {
                Email = "user@test.com",
                Password = "User123!"
            };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userRole = await db.UserRoles
                        .SingleOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                    if (userRole != null)
                    {
                        db.UserRoles.Remove(userRole);
                        await db.SaveChangesAsync();
                    }
                }
            }

            //Act
            var response = await PostJsonAsync("api/login", request);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task LoginAsyncTest_404()
        {
            //Arrange
            var request = new LoginRequest
            {
                Email = "BadUser@test.com",
                Password = "User123!"
            };

            //Act
            var response = await PostJsonAsync("api/login", request);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task LoginAsyncTest_423()
        {
            //Arrange
            var request = new LoginRequest
            {
                Email = "user@test.com",
                Password = "User123!"
            };
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.UtcNow.AddHours(1);
                db.Users.Update(user);
                await db.SaveChangesAsync();
            }

            //Act
            var response = await PostJsonAsync("api/login", request);

            //Assert returns unauth instead of locked
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        /*[Fact]
        public async Task RegisterNewUserTest_200OK()
        {
            var request = new RegisterNewUserRequest
            {
                UserName = "NewUser",
                Email = "newuser@test.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };
            using var client = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var tempProvider = services.BuildServiceProvider();
                    var logger = tempProvider.GetService<ILogger<Services.Helpers.EmailSender>>() ?? NullLogger<Services.Helpers.EmailSender>.Instance;
                    var config = tempProvider.GetService<IConfiguration>() ?? new ConfigurationBuilder().Build();
                    var mockEmailSender = new Mock<Services.Helpers.EmailSender>(logger, config, null);
                    mockEmailSender
                        .Setup(m => m.SendRegisterConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);
                    services.RemoveAll<Services.Helpers.EmailSender>();
                    services.AddSingleton(mockEmailSender.Object);
                });
            }).CreateClient();
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");
            var response = await client.PostAsync("api/register", content);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            Assert.True(root.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message));
            Assert.False(string.IsNullOrWhiteSpace(message.GetString()));
        }*/

        [Fact]
        public async Task LogoutAsyncTest_200OK()
        {
            //Arrange
            //-

            //Act
            var response = await _client.PostAsync("/api/logout", null);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success));
            Assert.True(success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message));
        }

        public async Task LogoutAsyncTest_401()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.PostAsync("/api/logout", null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshTokenAsyncTest_200OK()
        {
            //Arrange
            string refreshTokenValue;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                refreshTokenValue = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var rt = new Data.Models.RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = "127.0.0.1",
                    UserAgent = "test-agent",
                    IsRevoked = false
                };
                db.RefreshTokens.Add(rt);
                await db.SaveChangesAsync();
            }
            if (_client.DefaultRequestHeaders.Contains("Cookie"))
                _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", $"refresh_token={refreshTokenValue}");

            //Act
            var response = await _client.PostAsync("/api/refresh", null);
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(data);
            Assert.False(string.IsNullOrWhiteSpace(data.Email));
        }

        [Fact]
        public async Task RefreshTokenAsyncTest_401()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PostAsync("/api/refresh", null);

            //Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task RefreshTokenAsyncTest_403()
        {
            //Arrange
            string refreshTokenValue;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userRole = await db.UserRoles
                        .SingleOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                    if (userRole != null)
                    {
                        db.UserRoles.Remove(userRole);
                        await db.SaveChangesAsync();
                    }
                }
                refreshTokenValue = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
                var rt = new Data.Models.RefreshToken
                {
                    Id = Guid.NewGuid(),
                    UserId = user.Id,
                    Token = refreshTokenValue,
                    ExpiryDate = DateTime.UtcNow.AddDays(7),
                    CreatedAt = DateTime.UtcNow,
                    IpAddress = "127.0.0.1",
                    UserAgent = "test-agent",
                    IsRevoked = false
                };
                db.RefreshTokens.Add(rt);
                await db.SaveChangesAsync();
            }
            if (_client.DefaultRequestHeaders.Contains("Cookie"))
                _client.DefaultRequestHeaders.Remove("Cookie");
            _client.DefaultRequestHeaders.Add("Cookie", $"refresh_token={refreshTokenValue}");

            //Act
            var response = await _client.PostAsync("/api/refresh", null);
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);

            //Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUserAsync_200OK()
        {
            //Arrange
            //--

            //Act
            var response = await _client.GetAsync("api/me");
            var body = await response.Content.ReadAsStringAsync();
            var data = JsonSerializer.Deserialize<LoginResponse>(body, JsonOptions);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.NotNull(data);
            Assert.False(string.IsNullOrWhiteSpace(data.Email));
            Assert.False(string.IsNullOrWhiteSpace(data.UserName));
            Assert.NotNull(data.Roles);
            Assert.NotEmpty(data.Roles);
        }

        [Fact]
        public async Task GetCurrentUserAsync_400()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.GetAsync("api/me");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUserAsync_403()
        {
            //Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
                var role = await db.Roles.SingleOrDefaultAsync(r => r.Name == "User");
                if (role != null)
                {
                    var userRole = await db.UserRoles
                        .SingleOrDefaultAsync(ur => ur.UserId == user.Id && ur.RoleId == role.Id);
                    if (userRole != null)
                    {
                        db.UserRoles.Remove(userRole);
                        await db.SaveChangesAsync();
                    }
                }
                await db.SaveChangesAsync();
            }

            //Act
            var response = await _client.GetAsync("api/me");

            // Assert
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUserAsync_404()
        {
            //Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await db.Users.SingleAsync(u => u.Email == "user@test.com");
            db.Users.Remove(user);
            await db.SaveChangesAsync();

            //Act
            var response = await _client.GetAsync("api/me");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}