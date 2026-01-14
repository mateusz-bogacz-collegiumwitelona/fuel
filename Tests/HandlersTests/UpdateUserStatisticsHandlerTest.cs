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
    public class UpdateUserStatisticsHandlerTest
    {
        [Fact]
        public async Task HandleAsync_RepositoryReturnsTrue_LogsInformation()
        {
            // Arrange
            Mock<IProposalStatisticRepository> statsRepoMock = new Mock<IProposalStatisticRepository>();
            Mock<ILogger<UpdateUserStatisticsHandler>> loggerMock = new Mock<ILogger<UpdateUserStatisticsHandler>>();

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user"
            };

            Station station = new Station
            {
                Id = Guid.NewGuid()
            };

            PriceProposal proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id,
                Station = station,
                StationId = station.Id,
                ProposedPrice = 3.14m
            };

            PriceProposalEvaluatedEvent @event = new PriceProposalEvaluatedEvent(proposal, true);

            statsRepoMock
                .Setup(r => r.UpdateTotalProposalsAsync(true, user.Id))
                .ReturnsAsync(true);

            UpdateUserStatisticsHandler handler = new UpdateUserStatisticsHandler(statsRepoMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            statsRepoMock.Verify(r => r.UpdateTotalProposalsAsync(true, user.Id), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("statistics updated")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_RepositoryReturnsFalse_LogsWarning()
        {
            // Arrange
            Mock<IProposalStatisticRepository> statsRepoMock = new Mock<IProposalStatisticRepository>();
            Mock<ILogger<UpdateUserStatisticsHandler>> loggerMock = new Mock<ILogger<UpdateUserStatisticsHandler>>();

            ApplicationUser user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                UserName = "user2"
            };

            Station station = new Station
            {
                Id = Guid.NewGuid()
            };

            PriceProposal proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id,
                Station = station,
                StationId = station.Id,
                ProposedPrice = 6.28m
            };

            PriceProposalEvaluatedEvent @event = new PriceProposalEvaluatedEvent(proposal, false);

            statsRepoMock
                .Setup(r => r.UpdateTotalProposalsAsync(false, user.Id))
                .ReturnsAsync(false);

            UpdateUserStatisticsHandler handler = new UpdateUserStatisticsHandler(statsRepoMock.Object, loggerMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            statsRepoMock.Verify(r => r.UpdateTotalProposalsAsync(false, user.Id), Times.Once);

            loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to update user statistics")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()
                ),
                Times.Once);
        }
    }
}
