using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class ReportServiceTest
    {
        private readonly Mock<IReportRepositry> _reportRepoMock;
        private readonly Mock<ILogger<ReportService>> _loggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<IBanService> _banServiceMock;
        private readonly ReportService _service;
        private readonly ITestOutputHelper _output;

        public ReportServiceTest(ITestOutputHelper output)
        {
            _output = output;

            _reportRepoMock = new Mock<IReportRepositry>();
            _loggerMock = new Mock<ILogger<ReportService>>();

            var userStore = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                userStore.Object, null, null, null, null, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };

            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
            _roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };

            _banServiceMock = new Mock<IBanService>();

            _service = new ReportService(
                _reportRepoMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _banServiceMock.Object
            );
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsBadRequest_WhenReportedUserNameEmpty()
        {
            var result = await _service.ReportUserAsync("notifier@test.com", new ReportRequest { ReportedUserName = "", Reason = "r" });
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Validation: empty reported user name -> BadRequest");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsUnauthorized_WhenNotifierEmailEmpty()
        {
            var result = await _service.ReportUserAsync("", new ReportRequest { ReportedUserName = "user", Reason = "r" });
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
            _output.WriteLine("Validation: empty notifier email -> Unauthorized");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsNotFound_WhenNotifierNotFound()
        {
            var notifierEmail = "missing@x.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(notifierEmail)).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.ReportUserAsync(notifierEmail, new ReportRequest { ReportedUserName = "user", Reason = "r" });

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Notifier not found -> NotFound");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsNotFound_WhenReportedUserNotFound()
        {
            var notifier = new ApplicationUser { Id = Guid.NewGuid(), Email = "notifier@test.com" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(notifier.Email)).ReturnsAsync(notifier);
            _userManagerMock.Setup(u => u.FindByNameAsync("missingUser")).ReturnsAsync((ApplicationUser?)null);

            var result = await _service.ReportUserAsync(notifier.Email, new ReportRequest { ReportedUserName = "missingUser", Reason = "r" });

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            _output.WriteLine("Reported user not found -> NotFound");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsBadRequest_WhenReportingSelf()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@test.com", UserName = "user" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _userManagerMock.Setup(u => u.FindByNameAsync(user.UserName)).ReturnsAsync(user);

            var result = await _service.ReportUserAsync(user.Email, new ReportRequest { ReportedUserName = user.UserName, Reason = "r" });

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Reporting self -> BadRequest");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsBadRequest_WhenReportedIsAdmin()
        {
            var notifier = new ApplicationUser { Id = Guid.NewGuid(), Email = "notifier@test.com", UserName = "notifier" };
            var reported = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@test.com", UserName = "admin" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(notifier.Email)).ReturnsAsync(notifier);
            _userManagerMock.Setup(u => u.FindByNameAsync(reported.UserName)).ReturnsAsync(reported);
            _userManagerMock.Setup(u => u.IsInRoleAsync(reported, "Admin")).ReturnsAsync(true);

            var result = await _service.ReportUserAsync(notifier.Email, new ReportRequest { ReportedUserName = reported.UserName, Reason = "r" });

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            _output.WriteLine("Reporting admin -> BadRequest");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsInternalError_WhenRepositoryFails()
        {
            var notifier = new ApplicationUser { Id = Guid.NewGuid(), Email = "notifier@test.com", UserName = "notifier" };
            var reported = new ApplicationUser { Id = Guid.NewGuid(), Email = "reported@test.com", UserName = "reported" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(notifier.Email)).ReturnsAsync(notifier);
            _userManagerMock.Setup(u => u.FindByNameAsync(reported.UserName)).ReturnsAsync(reported);
            _userManagerMock.Setup(u => u.IsInRoleAsync(reported, "Admin")).ReturnsAsync(false);

            _reportRepoMock.Setup(r => r.ReportUserAsync(reported, notifier, It.IsAny<string>())).ReturnsAsync(false);

            var result = await _service.ReportUserAsync(notifier.Email, new ReportRequest { ReportedUserName = reported.UserName, Reason = "r" });

            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            _output.WriteLine("Repository failure -> InternalServerError");
        }

        [Fact]
        public async Task ReportUserAsync_ReturnsSuccess_WhenRepositorySucceeds()
        {
            var notifier = new ApplicationUser { Id = Guid.NewGuid(), Email = "notifier@test.com", UserName = "notifier" };
            var reported = new ApplicationUser { Id = Guid.NewGuid(), Email = "reported@test.com", UserName = "reported" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(notifier.Email)).ReturnsAsync(notifier);
            _userManagerMock.Setup(u => u.FindByNameAsync(reported.UserName)).ReturnsAsync(reported);
            _userManagerMock.Setup(u => u.IsInRoleAsync(reported, "Admin")).ReturnsAsync(false);

            _reportRepoMock.Setup(r => r.ReportUserAsync(reported, notifier, It.IsAny<string>())).ReturnsAsync(true);

            var result = await _service.ReportUserAsync(notifier.Email, new ReportRequest { ReportedUserName = reported.UserName, Reason = "valid reason" });

            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            _output.WriteLine("Successful report -> OK");
        }

        [Fact]
        public async Task GetUserReportAsync_ReturnsBadRequest_WhenEmailEmpty()
        {
            var res = await _service.GetUserReportAsync("", new GetPaggedRequest());
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, res.StatusCode);
            _output.WriteLine("GetUserReportAsync: empty email -> BadRequest");
        }

        [Fact]
        public async Task GetUserReportAsync_ReturnsNotFound_WhenUserNotFound()
        {
            var email = "notfound@test.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

            var res = await _service.GetUserReportAsync(email, new GetPaggedRequest());
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, res.StatusCode);
            _output.WriteLine("GetUserReportAsync: user not found -> NotFound");
        }

        [Fact]
        public async Task GetUserReportAsync_ReturnsEmptyPage_WhenNoReports()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "u@test.com" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            _reportRepoMock.Setup(r => r.GetUserReportAsync(user.Id)).ReturnsAsync(new List<UserReportsResponse>());

            var res = await _service.GetUserReportAsync(user.Email, new GetPaggedRequest { PageNumber = 1, PageSize = 10 });
            Assert.True(res.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, res.StatusCode);
            Assert.NotNull(res.Data);
            Assert.Empty(res.Data.Items);
            _output.WriteLine("GetUserReportAsync: no reports -> empty page OK");
        }

        [Fact]
        public async Task GetUserReportAsync_ReturnsPagedResult_WhenReportsExist()
        {
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "u@test.com" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(user.Email)).ReturnsAsync(user);

            var reports = new List<UserReportsResponse>
            {
                new UserReportsResponse { ReportingUserName = "r1", Reason = "x", CreatedAt = DateTime.UtcNow }
            };
            _reportRepoMock.Setup(r => r.GetUserReportAsync(user.Id)).ReturnsAsync(reports);

            var res = await _service.GetUserReportAsync(user.Email, new GetPaggedRequest { PageNumber = 1, PageSize = 10 });
            Assert.True(res.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, res.StatusCode);
            Assert.Single(res.Data.Items);
            _output.WriteLine("GetUserReportAsync: reports exist -> paged result OK");
        }

        [Fact]
        public async Task ChangeReportStatusAsync_ReturnsBadRequest_WhenIsAcceptedNull()
        {
            
            var req = new ChangeReportStatusRequest
            {
                
                ReportedUserEmail = null,
                ReportingUserEmail = null,
                ReportCreatedAt = default,
                Reason = null,
                Days = null
            };
            var res = await _service.ChangeReportStatusAsync("admin@test.com", req);
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, res.StatusCode);
            _output.WriteLine("ChangeReportStatusAsync: IsAccepted null -> BadRequest");
        }

        [Fact]
        public async Task ChangeReportStatusAsync_ReturnsNotFound_WhenAdminNotFound()
        {
            var adminEmail = "admin@test.com";
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync((ApplicationUser?)null);

            var res = await _service.ChangeReportStatusAsync(adminEmail, new ChangeReportStatusRequest { IsAccepted = false });
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, res.StatusCode);
            _output.WriteLine("ChangeReportStatusAsync: admin not found -> NotFound");
        }

        [Fact]
        public async Task ChangeReportStatusAsync_ReturnsForbidden_WhenAdminIsNotAdmin()
        {
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@test.com" };
            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(false);

            var res = await _service.ChangeReportStatusAsync(admin.Email, new ChangeReportStatusRequest { IsAccepted = false });
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status403Forbidden, res.StatusCode);
            _output.WriteLine("ChangeReportStatusAsync: caller not admin -> Forbidden");
        }

        [Fact]
        public async Task ChangeReportStatusAsync_ReturnsBadRequest_WhenAcceptButReasonMissing()
        {
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@test.com" };
            var reported = new ApplicationUser { Id = Guid.NewGuid(), Email = "reported@test.com" };
            var reporting = new ApplicationUser { Id = Guid.NewGuid(), Email = "reporting@test.com" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.FindByEmailAsync("reported@test.com")).ReturnsAsync(reported);
            _userManagerMock.Setup(u => u.FindByEmailAsync("reporting@test.com")).ReturnsAsync(reporting);

            _reportRepoMock.Setup(r => r.ChangeRepostStatusToAcceptedAsync(reported.Id, reporting.Id, admin, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            var req = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = reported.Email,
                ReportingUserEmail = reporting.Email,
                ReportCreatedAt = DateTime.UtcNow,
                Reason = null 
            };

            var res = await _service.ChangeReportStatusAsync(admin.Email, req);
            Assert.False(res.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, res.StatusCode);
            _output.WriteLine("Accepting report without ban reason -> BadRequest");
        }

        [Fact]
        public async Task ChangeReportStatusAsync_ReturnsSuccess_WhenAcceptAndBanSucceeds()
        {
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "a@test.com" };
            var reported = new ApplicationUser { Id = Guid.NewGuid(), Email = "reported@test.com" };
            var reporting = new ApplicationUser { Id = Guid.NewGuid(), Email = "reporting@test.com" };

            _userManagerMock.Setup(u => u.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);
            _userManagerMock.Setup(u => u.FindByEmailAsync(reported.Email)).ReturnsAsync(reported);
            _userManagerMock.Setup(u => u.FindByEmailAsync(reporting.Email)).ReturnsAsync(reporting);

            _reportRepoMock.Setup(r => r.ChangeRepostStatusToAcceptedAsync(reported.Id, reporting.Id, admin, It.IsAny<DateTime>()))
                .ReturnsAsync(true);

            _banServiceMock.Setup(b => b.LockoutUserAsync(admin.Email, It.IsAny<SetLockoutForUserRequest>()))
                .ReturnsAsync(Result<IdentityResult>.Good("ok", StatusCodes.Status200OK, IdentityResult.Success));

            var req = new ChangeReportStatusRequest
            {
                IsAccepted = true,
                ReportedUserEmail = reported.Email,
                ReportingUserEmail = reporting.Email,
                ReportCreatedAt = DateTime.UtcNow,
                Reason = "violation",
                Days = 1
            };

            var res = await _service.ChangeReportStatusAsync(admin.Email, req);
            Assert.True(res.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, res.StatusCode);
            _output.WriteLine("Accepting report + ban succeeded -> OK");
        }
    }
}
