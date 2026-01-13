using System;
using System.Threading.Tasks;
using Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Handlers;
using Services.Interfaces;
using Xunit;

namespace Tests.HandlersTests
{
    public class NotifyUserBanHandlerTest
    {
        [Fact]
        public async Task HandleAsync_EmailSent_NoWarningLogged()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            var loggerMock = new Mock<ILogger<NotifyUserBanHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "banned@example.com",
                UserName = "bannedUser"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "adminUser"
            };

            var @event = new UserBannedEvent(user, admin, "reason", 7);

            emailSenderMock
                .Setup(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, @event.Days, @event.Reason))
                .ReturnsAsync(true);

            var handler = new NotifyUserBanHandler(emailSenderMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, @event.Days, @event.Reason), Times.Once);

            loggerMock.Verify(l => l.Log(
                    It.Is<LogLevel>(ll => ll == LogLevel.Warning),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Never);
        }

        [Fact]
        public async Task HandleAsync_EmailSendFails_LogsWarning()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            var loggerMock = new Mock<ILogger<NotifyUserBanHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "banned2@example.com",
                UserName = "bannedUser2"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "adminUser2"
            };

            var @event = new UserBannedEvent(user, admin, "violation", null);

            emailSenderMock
                .Setup(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, @event.Days, @event.Reason))
                .ReturnsAsync(false);

            var handler = new NotifyUserBanHandler(emailSenderMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(e => e.SendLockoutEmailAsync(user.Email, user.UserName, admin.UserName, @event.Days, @event.Reason), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send lockout email to")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
