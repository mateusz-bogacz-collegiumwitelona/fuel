using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Helpers;
using Services.Services;
using Services.Interfaces;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class BanServiceTest
    {
        private readonly Mock<IBanRepository> _banRepoMock;
        private readonly Mock<IReportRepositry> _reportRepoMock;
        private readonly Mock<IEmailSender> _emailMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<ILogger<BanService>> _loggerMock;
        private readonly Mock<IConnectionMultiplexer> _redisMock;
        private readonly Mock<IDatabase> _dbMock;
        private readonly Mock<IServer> _serverMock;
        private readonly CacheService _cache;
        private readonly BanService _service;
        private readonly ITestOutputHelper _output;

        public BanServiceTest(ITestOutputHelper output)
        {
            _output = output;

            _banRepoMock = new Mock<IBanRepository>();
            _reportRepoMock = new Mock<IReportRepositry>();
            _emailMock = new Mock<IEmailSender>();
            _loggerMock = new Mock<ILogger<BanService>>();

            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(),
                null, null, null, null, null, null, null, null);

            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                Mock.Of<IRoleStore<IdentityRole<Guid>>>(),
                null, null, null, null);

          
            _redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            _dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            _serverMock = new Mock<IServer>(MockBehavior.Strict);

            _redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(_dbMock.Object);

            EndPoint[] endpoints = new EndPoint[] { new DnsEndPoint("127.0.0.1", 6379) };
            _redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(endpoints);
            _redisMock.Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>())).Returns(_serverMock.Object);

            _serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());

            _cache = new CacheService(_redisMock.Object, Mock.Of<ILogger<CacheService>>());

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
        public async Task LockoutUserAsync_ReturnsBadRequest_WhenEmailMissing()
        {
            var request = new SetLockoutForUserRequest { Email = "", Reason = "reason" };

            var result = await _service.LockoutUserAsync("admin@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync validates email");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsBadRequest_WhenReasonMissing()
        {
            var request = new SetLockoutForUserRequest { Email = "user@example.com", Reason = " " };

            var result = await _service.LockoutUserAsync("admin@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync validates reason");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsNotFound_WhenTargetUserNotFound()
        {
            var request = new SetLockoutForUserRequest { Email = "missing@example.com", Reason = "r" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.LockoutUserAsync("admin@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync returns 404 when target user not found");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsForbidden_WhenTargetIsAdmin()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin-target@example.com", UserName = "admintarget" };
            var request = new SetLockoutForUserRequest { Email = user.Email, Reason = "r" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(true);

            var result = await _service.LockoutUserAsync("superadmin@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync forbids banning an admin target");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsNotFound_WhenAdminMissing()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user" };
            var request = new SetLockoutForUserRequest { Email = user.Email, Reason = "r" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

            _userManagerMock.Setup(u => u.FindByEmailAsync("missing-admin@example.com")).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.LockoutUserAsync("missing-admin@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync returns 404 when calling admin not found");
        }

        [Fact]
        public async Task LockoutUserAsync_ReturnsForbidden_WhenCallerNotAdmin()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "caller@example.com", UserName = "caller" };
            var request = new SetLockoutForUserRequest { Email = user.Email, Reason = "r" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

            _userManagerMock.Setup(u => u.FindByEmailAsync("caller@example.com")).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(false);

            var result = await _service.LockoutUserAsync("caller@example.com", request);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync forbids when caller is not admin");
        }

        [Fact]
        public async Task LockoutUserAsync_Successful_BansUserAndRecordsBan()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "target@example.com", UserName = "target" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@example.com", UserName = "admin" };
            var request = new SetLockoutForUserRequest { Email = user.Email, Reason = "violation", Days = 3 };

            _userManagerMock.Setup(u => u.FindByEmailAsync(request.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);

            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            _banRepoMock.Setup(b => b.DeactivateActiveBansAsync(user.Id, admin.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(u => u.SetLockoutEnabledAsync(user, true)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, It.IsAny<DateTimeOffset>())).ReturnsAsync(IdentityResult.Success);
            _banRepoMock.Setup(b => b.AddBanRecordAsync(user, admin, request)).ReturnsAsync(true);
            _emailMock.Setup(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, request.Days, request.Reason)).ReturnsAsync(true);
            _reportRepoMock.Setup(r => r.ClearReports(user.Id, admin)).Returns(Task.CompletedTask);
            _dbMock.Invocations.Clear(); // clear any prior redis mock invocations

            var result = await _service.LockoutUserAsync(admin.Email, request);

            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            _output.WriteLine("Test passed: LockoutUserAsync bans user and records ban");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsBadRequest_WhenEmailMissing()
        {
            var result = await _service.UnlockUserAsync("admin@example.com", "");

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: UnlockUserAsync validates email");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsNotFound_WhenUserMissing()
        {
            _userManagerMock.Setup(u => u.FindByEmailAsync("missing@example.com")).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.UnlockUserAsync("admin@example.com", "missing@example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: UnlockUserAsync returns 404 when user missing");
        }

        [Fact]
        public async Task UnlockUserAsync_ReturnsBadRequest_WhenUserNotLockedOut()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "user" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@example.com", UserName = "admin" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(false);

       
            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            var result = await _service.UnlockUserAsync(admin.Email, user.Email);

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Test passed: UnlockUserAsync validates lockout state");
        }

        [Fact]
        public async Task UnlockUserAsync_Successful_UnlocksUserAndSendsEmail()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "locked@example.com", UserName = "locked" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@example.com", UserName = "admin" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.IsInRoleAsync(user, "Admin")).ReturnsAsync(false);
            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.IsLockedOutAsync(user)).ReturnsAsync(true);

            _banRepoMock.Setup(b => b.DeactivateActiveBansAsync(user.Id, admin.Id)).Returns(Task.CompletedTask);
            _userManagerMock.Setup(u => u.SetLockoutEndDateAsync(user, null)).ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(u => u.ResetAccessFailedCountAsync(user)).ReturnsAsync(IdentityResult.Success);
            _emailMock.Setup(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName)).ReturnsAsync(true);
            _reportRepoMock.Setup(r => r.ClearReports(user.Id, admin)).Returns(Task.CompletedTask);

            var result = await _service.UnlockUserAsync(admin.Email, user.Email);

            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            _output.WriteLine("Test passed: UnlockUserAsync unlocks user and sends email");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsUnauthorized_WhenEmailMissing()
        {
            var result = await _service.GetUserBanInfoAsync(" ");

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
            _output.WriteLine("Test passed: GetUserBanInfoAsync validates email");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsNotFound_WhenNoBanInfo()
        {
            _banRepoMock.Setup(b => b.GetUserBanInfoAsync("missing@example.com")).ReturnsAsync((ReviewUserBanResponses?)null);

            var result = await _service.GetUserBanInfoAsync("missing@example.com");

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Test passed: GetUserBanInfoAsync returns 404 when no ban info");
        }

        [Fact]
        public async Task GetUserBanInfoAsync_ReturnsData_WhenBanInfoExists()
        {
            var email = "a@b.com";
            var banInfo = new ReviewUserBanResponses
            {
                UserName = "user",
                Reason = "violation",
                BannedAt = DateTime.UtcNow,
                BannedUntil = DateTime.UtcNow.AddDays(1),
                BannedBy = "admin"
            };
            _banRepoMock.Setup(b => b.GetUserBanInfoAsync(email)).ReturnsAsync(banInfo);

            var result = await _service.GetUserBanInfoAsync(email);

            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(banInfo.UserName, result.Data.UserName);
            _output.WriteLine("Test passed: GetUserBanInfoAsync returns ban info");
        }
    }
}
