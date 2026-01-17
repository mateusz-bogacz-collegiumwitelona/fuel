using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;
using Data.Models;
using Data.Interfaces;
using DTO.Requests;
using Services.Services;
using Services.Event.Interfaces;
using Services.Helpers;
using Services.Interfaces;

namespace Tests.ServicesTests
{
    public class BanServiceTest
    {
        private static Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var store = new Mock<IUserStore<ApplicationUser>>();
            var mgr = new Mock<UserManager<ApplicationUser>>(
                store.Object,
                null, null, null, null, null, null, null, null);
            return mgr;
        }

        [Fact]
        public async Task LockoutUserAsync_Publishes_UserBannedEvent()
        {
            // Arrange
            var banRepoMock = new Mock<IBanRepository>();
            banRepoMock.Setup(x => x.DeactivateActiveBansAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                       .Returns(Task.CompletedTask);
            banRepoMock.Setup(x => x.AddBanRecordAsync(It.IsAny<ApplicationUser>(), It.IsAny<ApplicationUser>(), It.IsAny<SetLockoutForUserRequest>()))
                       .ReturnsAsync(true);

            var loggerMock = new Mock<ILogger<BanService>>();
            var userManagerMock = CreateUserManagerMock();
            var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                new Mock<IRoleStore<IdentityRole<Guid>>>().Object, null, null, null, null);

            var emailMock = new Mock<IEmailSender>();
            var reportRepoMock = new Mock<IReportRepositry>();

           
            var redisMock = new Mock<StackExchange.Redis.IConnectionMultiplexer>();
            var cacheLoggerMock = new Mock<ILogger<CacheService>>();
            var cacheMock = new Mock<CacheService>(redisMock.Object, cacheLoggerMock.Object);

            var eventDispatcherMock = new Mock<IEventDispatcher>();

            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.pl", UserName = "user1" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@example.pl", UserName = "admin" };

            userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);

            userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Email == user.Email), "Admin"))
                           .ReturnsAsync(false);
            userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Email == admin.Email), "Admin"))
                           .ReturnsAsync(true);

            userManagerMock.Setup(x => x.SetLockoutEnabledAsync(It.IsAny<ApplicationUser>(), It.IsAny<bool>())).ReturnsAsync(IdentityResult.Success);
            userManagerMock.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset>()))
                           .ReturnsAsync(IdentityResult.Success);

            eventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<Services.Event.Interfaces.IEvent>())).Returns(Task.CompletedTask);

            var service = new BanService(
                banRepoMock.Object,
                loggerMock.Object,
                userManagerMock.Object,
                roleManagerMock.Object,
                emailMock.Object,
                reportRepoMock.Object,
                cacheMock.Object,
                eventDispatcherMock.Object);

            var request = new SetLockoutForUserRequest
            {
                Email = user.Email,
                Reason = "violation",
                Days = 7
            };

            // Act
            var result = await service.LockoutUserAsync(admin.Email, request);

            // Assert
            Assert.True(result.IsSuccess);
            eventDispatcherMock.Verify(x => x.PublishAsync(It.Is<Services.Event.Interfaces.IEvent>(e => e.GetType().Name == "UserBannedEvent")), Times.Once);
        }

        [Fact]
        public async Task UnlockUserAsync_Publishes_UserUnlockedEvent()
        {
            // Arrange
            var banRepoMock = new Mock<IBanRepository>();
            banRepoMock.Setup(x => x.DeactivateActiveBansAsync(It.IsAny<Guid>(), It.IsAny<Guid>()))
                       .Returns(Task.CompletedTask);

            var loggerMock = new Mock<ILogger<BanService>>();
            var userManagerMock = CreateUserManagerMock();
            var roleManagerMock = new Mock<RoleManager<IdentityRole<Guid>>>(
                new Mock<IRoleStore<IdentityRole<Guid>>>().Object, null, null, null, null);

            var emailMock = new Mock<IEmailSender>();
            var reportRepoMock = new Mock<IReportRepositry>();

          
            var redisMock2 = new Mock<StackExchange.Redis.IConnectionMultiplexer>();
            var cacheLoggerMock2 = new Mock<ILogger<CacheService>>();
            var cacheMock = new Mock<CacheService>(redisMock2.Object, cacheLoggerMock2.Object);

            var eventDispatcherMock = new Mock<IEventDispatcher>();

            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.pl", UserName = "user1" };
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = "admin@example.pl", UserName = "admin" };

            userManagerMock.Setup(x => x.FindByEmailAsync(user.Email)).ReturnsAsync(user);
            userManagerMock.Setup(x => x.FindByEmailAsync(admin.Email)).ReturnsAsync(admin);

            userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Email == user.Email), "Admin"))
                           .ReturnsAsync(false);
            userManagerMock.Setup(x => x.IsInRoleAsync(It.Is<ApplicationUser>(u => u.Email == admin.Email), "Admin"))
                           .ReturnsAsync(true);

            userManagerMock.Setup(x => x.IsLockedOutAsync(It.IsAny<ApplicationUser>())).ReturnsAsync(true);
            userManagerMock.Setup(x => x.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
                           .ReturnsAsync(IdentityResult.Success);

            eventDispatcherMock.Setup(x => x.PublishAsync(It.IsAny<Services.Event.Interfaces.IEvent>())).Returns(Task.CompletedTask);

            var service = new BanService(
                banRepoMock.Object,
                loggerMock.Object,
                userManagerMock.Object,
                roleManagerMock.Object,
                emailMock.Object,
                reportRepoMock.Object,
                cacheMock.Object,
                eventDispatcherMock.Object);

            // Act
            var result = await service.UnlockUserAsync(admin.Email, user.Email);

            // Assert
            Assert.True(result.IsSuccess);
            eventDispatcherMock.Verify(x => x.PublishAsync(It.Is<Services.Event.Interfaces.IEvent>(e => e.GetType().Name == "UserUnlockedEvent")), Times.Once);
        }
    }
}
