using System;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Handlers;
using Xunit;

namespace Tests.HandlersTests
{
    public class InitializeUserStatsHandlerTest
    {
        [Fact]
        public async Task HandleAsync_RepositoryReturnsTrue_CallsRepoAndLogsInformation()
        {
            // Arrange
            Mock<IProposalStatisticRepository> proposalRepoMock = new Mock<IProposalStatisticRepository>();
            Mock<ILogger<InitializeUserStatsHandler>> loggerMock = new Mock<ILogger<InitializeUserStatsHandler>>();

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };

            proposalRepoMock
                .Setup(r => r.AddProposalStatisticRecordAsync(user))
                .ReturnsAsync(true);

            InitializeUserStatsHandler handler = new InitializeUserStatsHandler(proposalRepoMock.Object, loggerMock.Object);

            UserRegisteredEvent @event = new UserRegisteredEvent(user, "token");

            // Act
            await handler.HandleAsync(@event);

            // Assert
            proposalRepoMock.Verify(r => r.AddProposalStatisticRecordAsync(user), Times.Once);
            loggerMock.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Initialized stats for new user")),
                    It.Is<Exception>(ex => ex == null),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }

        [Fact]
        public async Task HandleAsync_RepositoryReturnsFalse_CallsRepoAndLogsWarning()
        {
            // Arrange
            Mock<IProposalStatisticRepository> proposalRepoMock = new Mock<IProposalStatisticRepository>();
            Mock<ILogger<InitializeUserStatsHandler>> loggerMock = new Mock<ILogger<InitializeUserStatsHandler>>();

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com"
            };

            proposalRepoMock
                .Setup(r => r.AddProposalStatisticRecordAsync(user))
                .ReturnsAsync(false);

            InitializeUserStatsHandler handler = new InitializeUserStatsHandler(proposalRepoMock.Object, loggerMock.Object);

            UserRegisteredEvent @event = new UserRegisteredEvent(user, "token");

            // Act
            await handler.HandleAsync(@event);

            // Assert
            proposalRepoMock.Verify(r => r.AddProposalStatisticRecordAsync(user), Times.Once);
            loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to initialize stats for new user")),
                    It.Is<Exception>(ex => ex == null),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        }
    }
}
