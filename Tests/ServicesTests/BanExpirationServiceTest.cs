using Data.Context;
using Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Services.BackgroundServices;
using Services.Email;
using Services.Helpers;
using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Tests.ServicesTests
{
    public class BanExpirationServiceTest
    {
        private static ServiceProvider BuildServiceProvider(
            string inMemoryDbName,
            Mock<UserManager<ApplicationUser>> userManagerMock,
            EmailSender emailSender)
        {
            var services = new ServiceCollection();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(inMemoryDbName));
            services.AddSingleton(userManagerMock.Object);
            services.AddSingleton(emailSender);
            services.AddSingleton<ILogger<BanExpirationService>>(new Mock<ILogger<BanExpirationService>>().Object);
            return services.BuildServiceProvider();
        }

        [Fact]
        public async Task CheckExpiredBansAsync_NoExpired_ShouldNotModifyBans()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            userManagerMock
                .Setup(u => u.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock
                .Setup(u => u.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var emailBody = new EmailBodys();
            var emailLogger = new Mock<ILogger<EmailSender>>().Object;
            var emailQueueMock = new Mock<IEmailQueue>().Object;
            var emailSender = new EmailSender(emailLogger, config, emailBody, emailQueueMock);

            using (var sp = BuildServiceProvider(dbName, userManagerMock, emailSender))
            {
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = "noexpire@example.com",
                        UserName = "noexpire",
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    var ban = new BanRecord
                    {
                        Id = Guid.NewGuid(),
                        User = user,
                        UserId = user.Id,
                        Reason = "Test",
                        BannedAt = DateTime.UtcNow,
                        BannedUntil = DateTime.UtcNow.AddDays(1),
                        IsActive = true,
                        AdminId = Guid.NewGuid()
                    };

                    ctx.Users.Add(user);
                    ctx.BanRecords.Add(ban);
                    await ctx.SaveChangesAsync();
                }

                var loggerMock = new Mock<ILogger<BanExpirationService>>();
                var service = new BanExpirationService(sp, loggerMock.Object);

                // Act
                var method = typeof(BanExpirationService)
                    .GetMethod("CheckExpiredBansAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(method);
                var task = (Task?)method!.Invoke(service, new object[] { CancellationToken.None });
                Assert.NotNull(task);
                await task!;

                // Assert
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var ban = ctx.BanRecords.Include(b => b.User).First();
                    Assert.True(ban.IsActive);
                    Assert.Null(ban.UnbannedAt);
                    userManagerMock.Verify(u => u.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), It.IsAny<DateTimeOffset?>()), Times.Never);
                    userManagerMock.Verify(u => u.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Never);
                }
            }
        }

        [Fact]
        public async Task CheckExpiredBansAsync_Expired_ShouldUnbanUserAndCallUserManager()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            userManagerMock
                .Setup(u => u.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null))
                .ReturnsAsync(IdentityResult.Success);
            userManagerMock
                .Setup(u => u.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(IdentityResult.Success);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var emailBody = new EmailBodys();
            var emailLogger = new Mock<ILogger<EmailSender>>().Object;
            var emailQueueMock = new Mock<IEmailQueue>().Object; // Dodaj mock kolejki email
            var emailSender = new EmailSender(emailLogger, config, emailBody, emailQueueMock);

            using (var sp = BuildServiceProvider(dbName, userManagerMock, emailSender))
            {
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var user = new ApplicationUser
                    {
                        Id = Guid.NewGuid(),
                        Email = "expired@example.com",
                        UserName = "expired",
                        CreatedAt = DateTime.UtcNow.AddDays(-2),
                        UpdatedAt = DateTime.UtcNow.AddDays(-2)
                    };

                    var ban = new BanRecord
                    {
                        Id = Guid.NewGuid(),
                        User = user,
                        UserId = user.Id,
                        Reason = "Violation",
                        BannedAt = DateTime.UtcNow.AddDays(-3),
                        BannedUntil = DateTime.UtcNow.AddDays(-1),
                        IsActive = true,
                        AdminId = Guid.NewGuid()
                    };

                    ctx.Users.Add(user);
                    ctx.BanRecords.Add(ban);
                    await ctx.SaveChangesAsync();
                }

                var loggerMock = new Mock<ILogger<BanExpirationService>>();
                var service = new BanExpirationService(sp, loggerMock.Object);

                // Act
                var method = typeof(BanExpirationService)
                    .GetMethod("CheckExpiredBansAsync", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(method);
                var task = (Task?)method!.Invoke(service, new object[] { CancellationToken.None });
                Assert.NotNull(task);
                await task!;

                // Assert
                using (var scope = sp.CreateScope())
                {
                    var ctx = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var ban = ctx.BanRecords.Include(b => b.User).First();
                    Assert.False(ban.IsActive);
                    Assert.NotNull(ban.UnbannedAt);
                    userManagerMock.Verify(u => u.SetLockoutEndDateAsync(It.IsAny<ApplicationUser>(), null), Times.Once);
                    userManagerMock.Verify(u => u.ResetAccessFailedCountAsync(It.IsAny<ApplicationUser>()), Times.Once);
                }
            }
        }

        [Fact]
        public async Task ExecuteAsync_AlreadyCancelledToken_CompletesImmediately()
        {
            // Arrange
            var dbName = Guid.NewGuid().ToString();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);

            var config = new ConfigurationBuilder().AddInMemoryCollection().Build();
            var emailBody = new EmailBodys();
            var emailLogger = new Mock<ILogger<EmailSender>>().Object;
            var emailQueueMock = new Mock<IEmailQueue>().Object; // Dodaj mock kolejki email
            var emailSender = new EmailSender(emailLogger, config, emailBody, emailQueueMock);

            using var sp = BuildServiceProvider(dbName, userManagerMock, emailSender);
            var loggerMock = new Mock<ILogger<BanExpirationService>>();
            var service = new BanExpirationService(sp, loggerMock.Object);

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            // Act & Assert
            var execTask = service.StartAsync(cts.Token);
            await execTask;
            await service.StopAsync(CancellationToken.None);
        }
    }
}
    
