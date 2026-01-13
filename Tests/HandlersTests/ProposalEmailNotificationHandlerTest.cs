using System;
using System.Threading.Tasks;
using Data.Models;
using DTO.Requests;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Handlers;
using Services.Interfaces;
using Xunit;

namespace Tests.HandlersTests
{
    public class ProposalEmailNotificationHandlerTest
    {
        [Fact]
        public async Task HandleAsync_AcceptedProposal_CallsEmailSenderWithCorrectData()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();

            var brand = new Brand { Id = Guid.NewGuid(), Name = "BrandX" };
            var address = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "Main",
                HouseNumber = "10",
                City = "CityX",
                PostalCode = "00-000"
            };
            var station = new Station
            {
                Id = Guid.NewGuid(),
                Brand = brand,
                Address = address
            };

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@example.com",
                UserName = "user1"
            };

            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id,
                Station = station,
                StationId = station.Id,
                ProposedPrice = 4.25m
            };

            var @event = new PriceProposalEvaluatedEvent(proposal, true);

            var handler = new ProposalEmailNotificationHandler(emailSenderMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(es => es.SendPriceProposalStatusEmail(
                    user.Email,
                    user.UserName,
                    true,
                    It.Is<FindStationRequest>(f =>
                        f.BrandName == brand.Name &&
                        f.Street == address.Street &&
                        f.HouseNumber == address.HouseNumber &&
                        f.City == address.City),
                    proposal.ProposedPrice),
                Times.Once);
        }

        [Fact]
        public async Task HandleAsync_RejectedProposal_CallsEmailSenderWithCorrectData()
        {
            // Arrange
            var emailSenderMock = new Mock<IEmailSender>();

            var brand = new Brand { Id = Guid.NewGuid(), Name = "BrandY" };
            var address = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "Second",
                HouseNumber = "22A",
                City = "CityY",
                PostalCode = "11-111"
            };
            var station = new Station
            {
                Id = Guid.NewGuid(),
                Brand = brand,
                Address = address
            };

            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@example.com",
                UserName = "user2"
            };

            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                UserId = user.Id,
                Station = station,
                StationId = station.Id,
                ProposedPrice = 5.50m
            };

            var @event = new PriceProposalEvaluatedEvent(proposal, false);

            var handler = new ProposalEmailNotificationHandler(emailSenderMock.Object);

            // Act
            await handler.HandleAsync(@event);

            // Assert
            emailSenderMock.Verify(es => es.SendPriceProposalStatusEmail(
                    user.Email,
                    user.UserName,
                    false,
                    It.Is<FindStationRequest>(f =>
                        f.BrandName == brand.Name &&
                        f.Street == address.Street &&
                        f.HouseNumber == address.HouseNumber &&
                        f.City == address.City),
                    proposal.ProposedPrice),
                Times.Once);
        }
    }
}
