using System;
using System.Threading.Tasks;
using Data.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Handlers;
using Data.Models;

using Xunit;

namespace Tests.HandlersTests
{
    public class ClearUserReportsHandlerTest
    {
        [Fact]
        public async Task HandleAsync_AdminFound_CallsClearReportsAndLogsInformation()
        {
            // Arrange
            var reportRepoMock = new Mock<IReportRepositry>();
            var loggerMock = new Mock<ILogger<ClearUserReportsHandler>>();

            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.local",
                UserName = "admin"
            };
            var reported = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.local",
                UserName = "reported"
            };

            var @event = new UserBannedEvent(reported, admin, "reason", 7);

            userManagerMock.Setup(u => u.FindByIdAsync(admin.Id.ToString()))
                .ReturnsAsync(admin);

            reportRepoMock.Setup(r => r.ClearReports(reported.Id, admin))
                .Returns(Task.CompletedTask)
                .Verifiable();

            var handler = new ClearUserReportsHandler(
                reportRepoMock.Object,
                userManagerMock.Object,
                loggerMock.Object
            );

            // Act
            await handler.HandleAsync(@event);

            // Assert
            reportRepoMock.Verify(r => r.ClearReports(reported.Id, admin), Times.Once);

            loggerMock.Verify(l => l.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Cleared pending reports for banned user")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_AdminNotFound_LogsWarningAndDoesNotCallClearReports()
        {
            // Arrange
            var reportRepoMock = new Mock<IReportRepositry>();
            var loggerMock = new Mock<ILogger<ClearUserReportsHandler>>();

            var store = new Mock<IUserStore<ApplicationUser>>();
            var userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.local",
                UserName = "admin"
            };
            var reported = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.local",
                UserName = "reported"
            };

            var @event = new UserBannedEvent(reported, admin, "reason", 7);

            userManagerMock.Setup(u => u.FindByIdAsync(admin.Id.ToString()))
                .ReturnsAsync((ApplicationUser?)null);

            var handler = new ClearUserReportsHandler(
                reportRepoMock.Object,
                userManagerMock.Object,
                loggerMock.Object
            );

            // Act
            await handler.HandleAsync(@event);

            // Assert
            reportRepoMock.Verify(r => r.ClearReports(It.IsAny<Guid>(), It.IsAny<ApplicationUser>()), Times.Never);

            loggerMock.Verify(l => l.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Could not find admin with ID")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
