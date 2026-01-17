using System;
using System.Reflection;
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
    public class SendRegistrationEmailHandlerTest
    {
        [Fact]
        public async Task HandleAsync_EmailSent_LogsInformation()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            var loggerMock = new Mock<ILogger<SendRegistrationEmailHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user"
            };
            const string token = "confirmation-token";

            emailSenderMock
                .Setup(es => es.SendRegisterConfirmEmailAsync(user.Email, user.UserName, token))
                .ReturnsAsync(true);

            var handler = new SendRegistrationEmailHandler(emailSenderMock.Object, loggerMock.Object);

           
            var loggerField = typeof(SendRegistrationEmailHandler)
                .GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
            loggerField?.SetValue(handler, loggerMock.Object);

            var @event = new UserRegisteredEvent(user, token);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(es => es.SendRegisterConfirmEmailAsync(user.Email, user.UserName, token), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Sent confirmation email to")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_EmailSendFails_LogsError()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();
            var loggerMock = new Mock<ILogger<SendRegistrationEmailHandler>>();

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                UserName = "user2"
            };
            const string token = "confirmation-token-2";

            emailSenderMock
                .Setup(es => es.SendRegisterConfirmEmailAsync(user.Email, user.UserName, token))
                .ReturnsAsync(false);

            var handler = new SendRegistrationEmailHandler(emailSenderMock.Object, loggerMock.Object);

           
            var loggerField = typeof(SendRegistrationEmailHandler)
                .GetField("_logger", BindingFlags.Instance | BindingFlags.NonPublic);
            loggerField?.SetValue(handler, loggerMock.Object);

            var @event = new UserRegisteredEvent(user, token);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(es => es.SendRegisterConfirmEmailAsync(user.Email, user.UserName, token), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to send confirmation email to")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.Once);
        }
    }
}
