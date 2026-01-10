using Azure.Core;
using Data.Context;
using DTO.Requests;
using DTO.Responses;
using FluentEmail.Core;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using RTools_NTS.Util;
using Services.Helpers;
using Services.Interfaces;
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

        [Fact]
        public async Task RegisterNewUserTest_201()
        {
            //Arrange
            var request = new RegisterNewUserRequest
            {
                UserName = "NewTestUser",
                Email = "newUser@test.com",
                Password = "NewPass123!",
                ConfirmPassword = "NewPass123!"
            };
            using var newClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var tempProvider = services.BuildServiceProvider();
                    var config = tempProvider.GetService<IConfiguration>()
                                 ?? new ConfigurationBuilder().Build();

                    var mockEmailSender = new Mock<IEmailSender>();
                    mockEmailSender
                        .Setup(m => m.SendRegisterConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);

                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            }).CreateClient();
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await newClient.PostAsync("api/register", content);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()));
        }

        [Fact]
        public async Task RegisterNewUserTest_404()
        {
            //Arrange
            var request = new RegisterNewUserRequest
            {
                UserName = "",
                Email = "",
                Password = "",
                ConfirmPassword = ""
            };
            using var newClient = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var tempProvider = services.BuildServiceProvider();
                    var config = tempProvider.GetService<IConfiguration>()
                                 ?? new ConfigurationBuilder().Build();

                    var mockEmailSender = new Mock<IEmailSender>();
                    mockEmailSender
                        .Setup(m => m.SendRegisterConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                        .ReturnsAsync(true);

                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            }).CreateClient();
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await newClient.PostAsync("api/register", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmEmailAsyncTest_200OK()
        {
            //Arrange
            string token;
            using (var scope = _factory.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var user = new Data.Models.ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "newUser",
                    NormalizedUserName = "NEWUSER",
                    Email = "new@test.com",
                    NormalizedEmail = "NEW@TEST.COM",
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await userManager.CreateAsync(user);
                await userManager.AddToRoleAsync(user, "User");
                token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            }

            var request = new ConfirmEmailRequest
            {
                Email = "new@test.com",
                Token = token
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("api/confirm-email", content);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()));
        }

        [Fact]
        public async Task ConfirmEmailAsyncTest_400()
        {
            //Arrange
            string token;
            using (var scope = _factory.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var user = new Data.Models.ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "newUser",
                    NormalizedUserName = "NEWUSER",
                    Email = "new@test.com",
                    NormalizedEmail = "NEW@TEST.COM",
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await userManager.CreateAsync(user);
                await userManager.AddToRoleAsync(user, "User");
                token = await userManager.GenerateEmailConfirmationTokenAsync(user);
            }

            var request = new ConfirmEmailRequest
            {
                Email = "new@test.com",
                Token = token
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("api/confirm-email", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ConfirmEmailAsyncTest_404()
        {
            //Arrange
            var request = new ConfirmEmailRequest
            {
                Email = "new@test.com",
                Token = "bad"
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await _client.PostAsync("api/confirm-email", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task ForgotPasswordAsyncTest_200OK()
        {
            //Arrange
            var mockEmailSender = new Mock<IEmailSender>();
            mockEmailSender
                .Setup(m => m.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            var factoryWithMock = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            });
            using (var scope = factoryWithMock.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var user = new Data.Models.ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = "newUser",
                        NormalizedUserName = "NEWUSER",
                        Email = "newuser@test.com",
                        NormalizedEmail = "newuser@test.com",
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                    var createResult = await userManager.CreateAsync(user, "Test123!");
                    Assert.True(createResult.Succeeded, "Nie udało się utworzyć użytkownika testowego dla reset-password test.");
                    if (!await roleManager.RoleExistsAsync("User"))
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = "User" });
                    await userManager.AddToRoleAsync(user, "User");
            }
            using var client = factoryWithMock.CreateClient();
            var request = Uri.EscapeDataString("newuser@test.com");

            //Act
            var response = await client.PostAsync($"api/reset-password?email={request}", null);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()));
            mockEmailSender.Verify(m => m.SendResetPasswordEmailAsync(
                It.Is<string>(e => e == "newuser@test.com"),
                It.IsAny<string>(),
                It.IsAny<string>()),
                Times.Once);
        }

        [Fact]
        public async Task ForgotPasswordAsyncTest_400()
        {
            //Arrange
            var mockEmailSender = new Mock<IEmailSender>();
            mockEmailSender
                .Setup(m => m.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            var factoryWithMock = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            });
            using (var scope = factoryWithMock.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var user = new Data.Models.ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "newUser",
                    NormalizedUserName = "NEWUSER",
                    Email = "newuser@test.com",
                    NormalizedEmail = "newuser@test.com",
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await userManager.CreateAsync(user, "Test123!");
                Assert.True(createResult.Succeeded, "Nie udało się utworzyć użytkownika testowego dla reset-password test.");
                if (!await roleManager.RoleExistsAsync("User"))
                    await roleManager.CreateAsync(new IdentityRole<Guid> { Name = "User" });
                await userManager.AddToRoleAsync(user, "User");
            }
            using var client = factoryWithMock.CreateClient();
            var request = Uri.EscapeDataString("newuser@test.com");

            //Act
            var response = await client.PostAsync($"api/reset-password?email={request}", null);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task ForgotPasswordAsyncTest_404()
        {
            //Arrange
            //--

            //Act
            var response = await _client.PostAsync($"api/reset-password?email=bad@test.com", null);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

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
        public async Task GetCurrentUserAsyncTest_200OK()
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
        public async Task GetCurrentUserAsyncTest_400()
        {
            //Arrange
            var unauthClient = _factory.CreateClient();

            //Act
            var response = await unauthClient.GetAsync("api/me");

            // Assert
            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUserAsyncTest_403()
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
        public async Task GetCurrentUserAsyncTest_404()
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

        [Fact]
        public async Task SetNewPasswordAsyncTest_200OK()
        {
            //Arrange
            var email = "reset@test.com";
            var newPassword = "NewPass123!";
            string token;
            var mockEmailSender = new Mock<IEmailSender>();
            mockEmailSender
                .Setup(m => m.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            var factoryWithMock = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            });
            using (var scope = factoryWithMock.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
                var user = new Data.Models.ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        UserName = "reset",
                        NormalizedUserName = "RESET",
                        Email = email,
                        NormalizedEmail = email.ToUpperInvariant(),
                        EmailConfirmed = true,
                        SecurityStamp = Guid.NewGuid().ToString(),
                        ConcurrencyStamp = Guid.NewGuid().ToString(),
                        CreatedAt = DateTime.UtcNow
                    };
                var createResult = await userManager.CreateAsync(user, "Temp123!");
                await userManager.AddToRoleAsync(user, "User");
                token = await userManager.GeneratePasswordResetTokenAsync(user);
            }
            using var resetClient = factoryWithMock.CreateClient();
            var request = new ResetPasswordRequest
            {
                Email = email,
                Password = newPassword,
                ConfirmPassword = newPassword,
                Token = Uri.EscapeDataString(token)
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await resetClient.PostAsync("api/set-new-password", content);
            var body = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;

            //Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(root.TryGetProperty("success", out var success) && success.GetBoolean());
            Assert.True(root.TryGetProperty("message", out var message) && !string.IsNullOrWhiteSpace(message.GetString()));
        }

        [Fact]
        public async Task SetNewPasswordAsyncTest_400()
        {
            //Arrange
            var email = "reset@test.com";
            var newPassword = "NewPass123!";
            string token;
            var mockEmailSender = new Mock<IEmailSender>();
            mockEmailSender
                .Setup(m => m.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            var factoryWithMock = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            });
            using (var scope = factoryWithMock.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
                var user = new Data.Models.ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "reset",
                    NormalizedUserName = "RESET",
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await userManager.CreateAsync(user, "Temp123!");
                await userManager.AddToRoleAsync(user, "User");
                token = await userManager.GeneratePasswordResetTokenAsync(user);
            }
            using var resetClient = factoryWithMock.CreateClient();
            var request = new ResetPasswordRequest
            {
                Email = "",
                Password = "",
                ConfirmPassword = "",
                Token = Uri.EscapeDataString(token)
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await resetClient.PostAsync("api/set-new-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task SetNewPasswordAsyncTest_404()
        {
            //Arrange
            var email = "reset@test.com";
            var newPassword = "NewPass123!";
            string token;
            var mockEmailSender = new Mock<IEmailSender>();
            mockEmailSender
                .Setup(m => m.SendResetPasswordEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);
            var factoryWithMock = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSender>();
                    services.AddSingleton<IEmailSender>(mockEmailSender.Object);
                });
            });
            using (var scope = factoryWithMock.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                var userManager = services.GetRequiredService<UserManager<Data.Models.ApplicationUser>>();
                var roleManager = services.GetRequiredService<RoleManager<IdentityRole<Guid>>>();
                var db = services.GetRequiredService<ApplicationDbContext>();
                var existingUser = await db.Users.SingleOrDefaultAsync(u => u.Email == email);
                var user = new Data.Models.ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = "reset",
                    NormalizedUserName = "RESET",
                    Email = email,
                    NormalizedEmail = email.ToUpperInvariant(),
                    EmailConfirmed = true,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    CreatedAt = DateTime.UtcNow
                };
                var createResult = await userManager.CreateAsync(user, "Temp123!");
                await userManager.AddToRoleAsync(user, "User");
                token = await userManager.GeneratePasswordResetTokenAsync(user);
            }
            using var resetClient = factoryWithMock.CreateClient();
            var request = new ResetPasswordRequest
            {
                Email = "bad@test.com",
                Password = "Asd123!",
                ConfirmPassword = "Asd123!",
                Token = Uri.EscapeDataString(token)
            };
            var json = JsonSerializer.Serialize(request);
            using var content = new StringContent(json, Encoding.UTF8, "application/json");

            //Act
            var response = await resetClient.PostAsync("api/set-new-password", content);

            //Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}