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
    public class NotifyUserUnlockHandlerTest
    {
        [Fact]
        public async Task HandleAsync_EmailSent_NoWarningLogged()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            var loggerMock = new Mock<ILogger<NotifyUserUnlockHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "unlocked@example.com",
                UserName = "unlockedUser"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "adminUser"
            };

            var @event = new UserUnlockedEvent(user, admin);

            emailSenderMock
                .Setup(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName))
                .ReturnsAsync(true);

            var handler = new NotifyUserUnlockHandler(emailSenderMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName), Times.Once);

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
            var loggerMock = new Mock<ILogger<NotifyUserUnlockHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "unlocked2@example.com",
                UserName = "unlockedUser2"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "adminUser2"
            };

            var @event = new UserUnlockedEvent(user, admin);

            emailSenderMock
                .Setup(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName))
                .ReturnsAsync(false);

            var handler = new NotifyUserUnlockHandler(emailSenderMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(e => e.SendUnlockEmailAsync(user.Email, user.UserName, admin.UserName), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send unlock email to")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
