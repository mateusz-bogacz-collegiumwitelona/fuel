using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Helpers;
using Services.Services;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class BanServiceTest
    {
        private readonly Mock<IBanRepository> _banRepoMock;
        private readonly Mock<IReportRepositry> _reportRepoMock;
        private readonly Mock<ILogger<BanService>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<EmailSender> _emailMock;
        private readonly CacheService _cache;
        private readonly BanService _service;
        private readonly ITestOutputHelper _output;

        public BanServiceTest(ITestOutputHelper output)
        {
            _output = output;

            _banRepoMock = new Mock<IBanRepository>();
            _reportRepoMock = new Mock<IReportRepositry>();
            _loggerMock = new Mock<ILogger<BanService>>();
          
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Frontend:Url"] = "http://localhost:4000",
                ["Mail:Host"] = "",
                ["Mail:Port"] = "1025",
                ["Mail:EnableSsl"] = "false",
                ["Mail:From"] = ""
            };

            var configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            _emailMock = new Mock<EmailSender>(
                MockBehavior.Strict,
                Mock.Of<ILogger<EmailSender>>(),
                configuration,
                new Services.Helpers.EmailBodys()
            );

          
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                Mock.Of<Microsoft.Extensions.Options.IOptions<IdentityOptions>>(),
                Mock.Of<IPasswordHasher<ApplicationUser>>(),
                Array.Empty<IUserValidator<ApplicationUser>>(),
                Array.Empty<IPasswordValidator<ApplicationUser>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<IServiceProvider>(),
                Mock.Of<ILogger<UserManager<ApplicationUser>>>()
            ) { DefaultValue = DefaultValue.Mock };

            
            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object,
                Array.Empty<IRoleValidator<IdentityRole<Guid>>>(),
                Mock.Of<ILookupNormalizer>(),
                Mock.Of<IdentityErrorDescriber>(),
                Mock.Of<ILogger<RoleManager<IdentityRole<Guid>>>>()
            ) { DefaultValue = DefaultValue.Mock };

           
            var redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            var serverMock = new Mock<IServer>(MockBehavior.Loose);
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(serverMock.Object);
            serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());

            _cache = new CacheService(redisMock.Object, Mock.Of<ILogger<CacheService>>());

            _service = new BanService(
                _banRepoMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _emailMock.Object,
                _reportRepoMock.Object,
                _cache
            );
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsBad_WhenRequestEmailMissing()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var request = new SetLockoutForUserRequest { Email = "", Reason = "reason", Days = 3 };

            // Act
            var result = await _service.LockoutUserAsync(adminEmail, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Lockout returns 400 when request email missing");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsNotFound_WhenUserMissing()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var request = new SetLockoutForUserRequest { Email = "missing@example.com", Reason = "reason", Days = 3 };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.LockoutUserAsync(adminEmail, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            _output.WriteLine("Lockout returns 404 when target user missing");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsForbidden_WhenTargetIsAdmin()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var request = new SetLockoutForUserRequest { Email = "target@example.com", Reason = "reason", Days = 3 };
            var targetUser = new ApplicationUser { Id = Guid.NewGuid(), Email = request.Email };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(targetUser);
            _userManagerMock.Setup(u => u.IsInRoleAsync(targetUser, "Admin")).ReturnsAsync(true);

            // Act
            var result = await _service.LockoutUserAsync(adminEmail, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(403, result.StatusCode);
            _output.WriteLine("Lockout returns 403 when trying to ban an admin");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsSuccess_WhenAllDepsSucceed()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var request = new SetLockoutForUserRequest { Email = "user@example.com", Reason = "violation", Days = 7 };
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = request.Email, UserName = "target" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = adminEmail, UserName = "admin" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            _banRepoMock.Setup(r => r.DeactivateActiveBansAsync(user.Id, admin.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(u => u.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
            _banRepoMock.Setup(r => r.AddBanRecordAsync(user, admin, request)).ReturnsAsync(true);
            _emailMock.Setup(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, request.Days, request.Reason)).ReturnsAsync(true);
            _reportRepoMock.Setup(r => r.ClearReports(user.Id, admin)).Returns(Task.CompletedTask);
            // cache remove invoked internally; CacheService uses mocks so it will succeed

            // Act
            var result = await _service.LockoutUserAsync(adminEmail, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Contains("banned", result.Message, StringComparison.OrdinalIgnoreCase);
            _output.WriteLine("Lockout success path returned 200.");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsBad_WhenUserEmailMissing()
        {
            // Arrange
            var adminEmail = "admin@example.com";

            // Act
            var result = await _service.UnlockUserAsync(adminEmail, "");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Unlock returns 400 when userEmail missing");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsNotFound_WhenUserMissing()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var userEmail = "missing@example.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(userEmail)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.UnlockUserAsync(adminEmail, userEmail);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            _output.WriteLine("Unlock returns 404 when target user missing");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsBad_WhenUserNotLockedOut()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var userEmail = "user@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = userEmail };

            _userManagerMock.Setup(u => u.FindByEmailAsync(userEmail)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync(new ApplicationUser { Id = Guid.NewGuid(), Email = adminEmail });
            _userManagerMock.Setup(u => u.IsInRoleAsync(It.IsAny<ApplicationUser>(), "Admin")).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);

            // Act
            var result = await _service.UnlockUserAsync(adminEmail, userEmail);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Unlock returns 400 when user is not locked out");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsSuccess_WhenAllDepsSucceed()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var userEmail = "user@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = userEmail, UserName = "t" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = adminEmail, UserName = "admin" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(userEmail)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            _userManagerMock.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(true);
            _banRepoMock.Setup(r => r.DeactivateActiveBansAsync(user.Id, admin.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
            _emailMock.Setup(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName)).ReturnsAsync(true);

            // Act
            var result = await _service.UnlockUserAsync(adminEmail, userEmail);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            _output.WriteLine("Unlock success path returned 200.");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsUnauthorized_WhenEmailMissing()
        {
            // Act
            var result = await _service.GetUserBanInfoAsync("");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            _output.WriteLine("GetUserBanInfo returns 401 when email missing");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsNotFound_WhenRepoReturnsNull()
        {
            // Arrange
            var email = "noban@example.com";
            _banRepoMock.Setup(r => r.GetUserBanInfoAsync(email)).ReturnsAsync((ReviewUserBanResponses?)null!);

            // Act
            var result = await _service.GetUserBanInfoAsync(email);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            _output.WriteLine("GetUserBanInfo returns 404 when repo returns null");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsSuccess_WhenFound()
        {
            // Arrange
            var email = "banned@example.com";
            var response = new ReviewUserBanResponses { UserName = "u", Reason = "x", BannedAt = DateTime.UtcNow, BannedBy = "admin" };
            _banRepoMock.Setup(r => r.GetUserBanInfoAsync(email)).ReturnsAsync(response);

            // Act
            var result = await _service.GetUserBanInfoAsync(email);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(response, result.Data);
            _output.WriteLine("GetUserBanInfo returns 200 and data when found");
        }
    }
}
