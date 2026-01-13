using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event;
using Services.Event.Interfaces;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using StackExchange.Redis;
using Xunit;
using Xunit.Abstractions;

namespace Tests.ServicesTests
{
    public class PriceProposalServicesTest
    {
        private readonly Mock<IPriceProposalRepository> _priceProposalRepoMock;
        private readonly Mock<IStationRepository> _stationRepoMock;
        private readonly Mock<IFuelTypeRepository> _fuelTypeRepoMock;
        private readonly Mock<IProposalStatisticRepository> _proposalStatisticRepoMock;
        private readonly Mock<ILogger<PriceProposalServices>> _loggerMock;
        private readonly Mock<ILogger<CacheService>> _cacheLoggerMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<IEmailSender> _emailMock;
        private readonly Mock<IEventDispatcher> _eventDispatcherMock;
        private readonly CacheService _cache;
        private readonly PriceProposalServices _service;
        private readonly ITestOutputHelper _output;

        public PriceProposalServicesTest(ITestOutputHelper output)
        {
            _output = output;

            _priceProposalRepoMock = new Mock<IPriceProposalRepository>();
            _stationRepoMock = new Mock<IStationRepository>();
            _fuelTypeRepoMock = new Mock<IFuelTypeRepository>();
            _proposalStatisticRepoMock = new Mock<IProposalStatisticRepository>();
            _loggerMock = new Mock<ILogger<PriceProposalServices>>();
            _cacheLoggerMock = new Mock<ILogger<CacheService>>();

            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Frontend:Url"] = "http://localhost:4000",
                ["Mail:Host"] = "",
                ["Mail:Port"] = "1025",
                ["Mail:EnableSsl"] = "false",
                ["Mail:From"] = ""
            };
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            // Mock interfejsu IEmailSender — prostsze i bez zależności konstrukcyjnych
            _emailMock = new Mock<IEmailSender>(MockBehavior.Strict);

            _eventDispatcherMock = new Mock<IEventDispatcher>(MockBehavior.Strict);

            var redisMock = new Mock<IConnectionMultiplexer>(MockBehavior.Strict);
            var dbMock = new Mock<IDatabase>(MockBehavior.Loose);
            var serverMock = new Mock<IServer>(MockBehavior.Loose);
            redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            redisMock.Setup(r => r.GetEndPoints(It.IsAny<bool>())).Returns(new EndPoint[] { endpoint });
            redisMock.Setup(r => r.GetServer(endpoint, It.IsAny<object>())).Returns(serverMock.Object);
            serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());

            _cache = new CacheService(redisMock.Object, _cacheLoggerMock.Object);

            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, null, null, null, null, null, null, null, null
            ) { DefaultValue = DefaultValue.Mock };

            _service = new PriceProposalServices(
                _priceProposalRepoMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object,
                _stationRepoMock.Object,
                _fuelTypeRepoMock.Object,
                _proposalStatisticRepoMock.Object,
                _emailMock.Object,
                _cache,
                _eventDispatcherMock.Object
            );
        }

        private IFormFile CreateFormFile(string fileName, int lengthBytes)
        {
            var content = new byte[lengthBytes];
            new Random(0).NextBytes(content);
            var stream = new MemoryStream(content);
            var fileMock = new Mock<IFormFile>();
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(lengthBytes);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            fileMock.Setup(f => f.ContentType).Returns("image/jpeg");
            return fileMock.Object;
        }

        [Fact]
        public async Task AddNewProposalAsync_ReturnsBad_WhenInvalidFileType()
        {
            // Arrange
            var email = "user@example.com";
            var request = new AddNewPriceProposalRequest
            {
                Photo = CreateFormFile("malicious.exe", 10),
                FuelTypeCode = "DIESEL",
                BrandName = "Any",
                Street = "S",
                HouseNumber = "1",
                City = "City",
                ProposedPrice = 3.99m
            };

            // Act
            var result = await _service.AddNewProposalAsync(email, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Invalid file type returns 400 as expected.");
        }

        [Fact]
        public async Task AddNewProposalAsync_ReturnsBad_WhenUserNotFound()
        {
            // Arrange
            var email = "missing@example.com";
            var request = new AddNewPriceProposalRequest
            {
                Photo = CreateFormFile("photo.jpg", 100),
                FuelTypeCode = "DIESEL",
                BrandName = "Any",
                Street = "S",
                HouseNumber = "1",
                City = "City",
                ProposedPrice = 3.99m
            };

            _fuelTypeRepoMock.Setup(f => f.GetAllFuelTypeCodesAsync()).ReturnsAsync(new List<string> { "DIESEL" });
            _fuelTypeRepoMock.Setup(f => f.FindFuelTypeByCodeAsync(It.IsAny<string>())).ReturnsAsync(new FuelType { Id = Guid.NewGuid(), Code = "DIESEL" });

            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.AddNewProposalAsync(email, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Missing user returns 400 as expected.");
        }

        [Fact]
        public async Task AddNewProposalAsync_ReturnsSuccess_WhenEverythingValid()
        {
            // Arrange
            var email = "ok@example.com";
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = "user1" };
            var fuelType = new FuelType { Id = Guid.NewGuid(), Code = "DIESEL" };
            var station = new Station
            {
                Id = Guid.NewGuid(),
                Brand = new Brand { Name = "Brand" },
                Address = new StationAddress { Street = "S", HouseNumber = "1", City = "City" }
            };

            var request = new AddNewPriceProposalRequest
            {
                Photo = CreateFormFile("photo.jpg", 100),
                FuelTypeCode = "diesel",
                BrandName = station.Brand.Name,
                Street = station.Address.Street,
                HouseNumber = station.Address.HouseNumber,
                City = station.Address.City,
                ProposedPrice = 4.50m
            };

            _fuelTypeRepoMock.Setup(f => f.GetAllFuelTypeCodesAsync()).ReturnsAsync(new List<string> { "DIESEL" });
            _fuelTypeRepoMock.Setup(f => f.FindFuelTypeByCodeAsync("DIESEL")).ReturnsAsync(fuelType);
            _userManagerMock.Setup(u => u.FindByEmailAsync(email)).ReturnsAsync(user);
            _stationRepoMock.Setup(s => s.FindStationByDataAsync(request.BrandName, request.Street, request.HouseNumber, request.City)).ReturnsAsync(station);

            _priceProposalRepoMock.Setup(p => p.AddNewPriceProposalAsync(
                It.IsAny<ApplicationUser>(),
                It.IsAny<Station>(),
                It.IsAny<FuelType>(),
                It.IsAny<decimal>(),
                It.IsAny<IFormFile>(),
                It.IsAny<string>()
            )).ReturnsAsync(true);

            // Act
            var result = await _service.AddNewProposalAsync(email, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            _output.WriteLine("AddNewProposalAsync success path returned 200.");
        }

        [Fact]
        public async Task GetPriceProposal_ReturnsBad_WhenTokenNullOrEmpty()
        {
            // Act
            var result = await _service.GetPriceProposal(string.Empty);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(400, result.StatusCode);
            _output.WriteLine("Empty token returns 400.");
        }

        [Fact]
        public async Task GetPriceProposal_ReturnsNotFound_WhenRepositoryReturnsNull()
        {
            // Arrange
            var token = "missing-token";
            _priceProposalRepoMock.Setup(p => p.GetPriceProposal(token)).ReturnsAsync((GetPriceProposalResponse?)null);

            // Act
            var result = await _service.GetPriceProposal(token);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(404, result.StatusCode);
            _output.WriteLine("Missing proposal returns 404.");
        }

        [Fact]
        public async Task GetPriceProposal_ReturnsSuccess_WhenFound()
        {
            // Arrange
            var token = "token-123";
            var response = new GetPriceProposalResponse { Token = token, Email = "u@e.com" };
            _priceProposalRepoMock.Setup(p => p.GetPriceProposal(token)).ReturnsAsync(response);

            // Act
            var result = await _service.GetPriceProposal(token);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(response, result.Data);
            _output.WriteLine("Found proposal returns 200 and data.");
        }

        [Fact]
        public async Task ChangePriceProposalStatus_ReturnsUnauthorized_WhenAdminNotInRole()
        {
            // Arrange
            var adminEmail = "notadmin@example.com";
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = adminEmail };
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(false);

            // Act
            var result = await _service.ChangePriceProposalStatus(adminEmail, true, "token");

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(401, result.StatusCode);
            _output.WriteLine("Non-admin returns 401.");
        }

        [Fact]
        public async Task ChangePriceProposalStatus_ReturnsSuccess_WhenAcceptedAndAllDepsSucceed()
        {
            // Arrange
            var adminEmail = "admin@example.com";
            var admin = new ApplicationUser { Id = Guid.NewGuid(), Email = adminEmail };
            _userManagerMock.Setup(u => u.FindByEmailAsync(adminEmail)).ReturnsAsync(admin);
            _userManagerMock.Setup(u => u.IsInRoleAsync(admin, "Admin")).ReturnsAsync(true);

            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "user@example.com", UserName = "userA" };
            var station = new Station
            {
                Id = Guid.NewGuid(),
                Brand = new Brand { Name = "B" },
                Address = new StationAddress { Street = "S", HouseNumber = "1", City = "C" }
            };

            var priceProposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = user,
                Station = station,
                ProposedPrice = 4.00m,
                ReviewedBy = null,
                Token = "tok-1"
            };

            _priceProposalRepoMock.Setup(p => p.FindPriceProposal("tok-1")).ReturnsAsync(priceProposal);
            _priceProposalRepoMock.Setup(p => p.ChangePriceProposalStatus(true, priceProposal, admin)).ReturnsAsync(true);
            _proposalStatisticRepoMock.Setup(p => p.UpdateTotalProposalsAsync(true, user.Id)).ReturnsAsync(true);

            
            _emailMock.Setup(e => e.SendPriceProposalStatusEmail(
                user.Email,
                user.UserName,
                true,
                It.IsAny<FindStationRequest>(),
                priceProposal.ProposedPrice
            )).ReturnsAsync(true);

           
            _eventDispatcherMock.Setup(ed => ed.PublishAsync(It.IsAny<PriceProposalEvaluatedEvent>())).Returns(Task.CompletedTask);

            // Act
            var result = await _service.ChangePriceProposalStatus(adminEmail, true, "tok-1");

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.True(result.Data);
            _output.WriteLine("ChangePriceProposalStatus success path returned 200.");
        }

        [Fact]
        public async Task GetPriceProposalStaisticAsync_ReturnsBad_WhenRepositoryReturnsNull()
        {
            // Arrange
            _priceProposalRepoMock.Setup(p => p.GetPriceProposalStaisticAsync()).ReturnsAsync((GetPriceProposalStaisticResponse?)null);

            // Act
            var result = await _service.GetPriceProposalStaisticAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            _output.WriteLine("Repository null returns 500 as expected.");
        }

        [Fact]
        public async Task GetPriceProposalStaisticAsync_ReturnsSuccess_WhenRepositoryReturnsData()
        {
            // Arrange
            var stats = new GetPriceProposalStaisticResponse
            {
                AcceptedRate = 5,
                RejectedRate = 2,
                PendingRate = 3
            };
            _priceProposalRepoMock.Setup(p => p.GetPriceProposalStaisticAsync()).ReturnsAsync(stats);

            // Act
            var result = await _service.GetPriceProposalStaisticAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.Same(stats, result.Data);
            _output.WriteLine("Repository returned stats and service returned 200 with data.");
        }

        [Fact]
        public async Task GetPriceProposalStaisticAsync_ReturnsBad_WhenRepositoryThrowsException()
        {
            // Arrange
            _priceProposalRepoMock.Setup(p => p.GetPriceProposalStaisticAsync()).ThrowsAsync(new Exception("db failure"));

            // Act
            var result = await _service.GetPriceProposalStaisticAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            _output.WriteLine("Repository exception results in 500.");
        }

        [Fact]
        public async Task GetAllPriceProposal_ReturnsEmptyPage_WhenRepositoryReturnsEmpty()
        {
            // Arrange
            var pagged = new GetPaggedRequest { PageNumber = 1, PageSize = 10 };
            var tableRequest = new TableRequest();
            _priceProposalRepoMock.Setup(p => p.GetAllPriceProposal(It.IsAny<TableRequest>()))
                .ReturnsAsync(new List<GetStationPriceProposalResponse>());

            // Act
            var result = await _service.GetAllPriceProposal(pagged, tableRequest);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(0, result.Data.TotalCount);
            Assert.Empty(result.Data.Items);
            _output.WriteLine("Empty repository result returns empty paged result with 200.");
        }

        [Fact]
        public async Task GetAllPriceProposal_ReturnsPagedResult_WhenRepositoryReturnsItems()
        {
            // Arrange
            var pagged = new GetPaggedRequest { PageNumber = 2, PageSize = 10 };
            var tableRequest = new TableRequest();
            var items = Enumerable.Range(1, 15)
                .Select(i => new GetStationPriceProposalResponse { Token = $"t{i}", UserName = $"u{i}" })
                .ToList();

            _priceProposalRepoMock.Setup(p => p.GetAllPriceProposal(It.IsAny<TableRequest>()))
                .ReturnsAsync(items);

            // Act
            var result = await _service.GetAllPriceProposal(pagged, tableRequest);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(200, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(15, result.Data.TotalCount);
            Assert.Equal(2, result.Data.TotalPages);
            Assert.Equal(2, result.Data.PageNumber);
            Assert.Equal(5, result.Data.Items.Count);
            _output.WriteLine("Repository returned 15 items and service paginated to page 2 with 5 items.");
        }

        [Fact]
        public async Task GetAllPriceProposal_ReturnsServerError_WhenRepositoryThrows()
        {
            // Arrange
            var pagged = new GetPaggedRequest { PageNumber = 1, PageSize = 10 };
            var tableRequest = new TableRequest();
            _priceProposalRepoMock.Setup(p => p.GetAllPriceProposal(It.IsAny<TableRequest>()))
                .ThrowsAsync(new Exception("db failure"));

            // Act
            var result = await _service.GetAllPriceProposal(pagged, tableRequest);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(500, result.StatusCode);
            _output.WriteLine("Repository exception results in 500 as expected.");
        }
    }
}
