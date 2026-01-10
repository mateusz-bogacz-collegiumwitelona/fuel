using Data.Context;
using Data.Enums;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using Data.Repositories;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.IO;
using System.Text;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class PriceProposalRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly PriceProposalRepository _repository;
        private readonly Mock<ILogger<PriceProposalRepository>> _loggerMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly ITestOutputHelper _output;
        private readonly Mock<IStorage> _storage;

        private readonly string _bucket = "proposal";
        private readonly Brand _brand = new() { Id= Guid.NewGuid(), Name = "Brand"};
        private readonly FuelType _fuel = new() { Id = Guid.NewGuid(), Code = "ON", Name = "Diesel"};
        private readonly StationAddress _address;
        private readonly ApplicationUser _user = new() { Id = Guid.NewGuid(), Email = "user@test.com" };
        private readonly Station _station;

        public PriceProposalRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            _storage = new Mock<IStorage>();
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<PriceProposalRepository>>();
            _configMock = new Mock<IConfiguration>();

            _address = new StationAddress
            {
                Id = Guid.NewGuid(),
                Street = "TestStreet",
                HouseNumber = "1",
                City = "TestCity",
                Location = geometryFactory.CreatePoint(new Coordinate(10.0, 10.0))
            };

            _station = new Station
            {
                Id = Guid.NewGuid(),
                Brand = _brand,
                BrandId = _brand.Id,
                Address = _address,
                AddressId = _address.Id
            };

            _repository = new PriceProposalRepository(_context, _loggerMock.Object, _storage.Object, _configMock.Object);

            _context.Users.Add(_user);
            _context.StationAddress.Add(_address);
            _context.FuelTypes.Add(_fuel);
            _context.Brand.Add(_brand);
            _context.Stations.Add(_station);
            _configMock.Setup(x => x["MinIO:BucketName"]).Returns(_bucket);

            _context.SaveChanges();
        }

        private Mock<IFormFile> MockFile(string extension, string content = "test")
        {
            var fileMock = new Mock<IFormFile>();
            var fileName = $"file{extension}";
            var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));
            fileMock.Setup(f => f.FileName).Returns(fileName);
            fileMock.Setup(f => f.Length).Returns(stream.Length);
            fileMock.Setup(f => f.OpenReadStream()).Returns(stream);
            return fileMock;
        }
        
        [Fact]
        public async Task AddNewPriceProposalAsyncTest_SuccessIfFileUploaded()
        {
            //Arrange
            var extension = ".png";
            var file = MockFile(extension);
            var url = "http://blob/test.jpg";
            _storage.Setup(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            _bucket,
            It.IsAny<string>()
            )).ReturnsAsync(url);
            //Act
            var result = await _repository.AddNewPriceProposalAsync(_user, _station, _fuel, 5.0m, file.Object, extension);

            //Assert
            Assert.True(result);
            _storage.Verify(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            _bucket,
            It.IsAny<string>()
            ),
            Times.Once());
            Assert.Equal(5.0m, _context.PriceProposals.ToList().First().ProposedPrice);
            _output.WriteLine("Success, AddNewPriceProposalAsync uploads a proposal");
        }

        [Fact]
        public async Task AddNewPriceProposalAsyncTest_BadExtension_SuccessIfPriceProposalsEmpty()
        {
            //Arrange
            var extension = ".mp4";
            var file = MockFile(extension);
            var url = "http://blob/test.jpg";
            _storage.Setup(x => x.UploadFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>(),
            _bucket,
            It.IsAny<string>()
            )).ReturnsAsync(url);
            //Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddNewPriceProposalAsync(_user, _station, _fuel, 5.0m, file.Object, extension));

            //Assert
            Assert.Empty(_context.PriceProposals.ToList());
            _storage.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _output.WriteLine("Success, AddNewPriceProposalAsyncTest doesnt add a proposal when it gets an unsuported extension");
        }

        [Fact]
        public async Task AddNewPriceProposalAsyncTest_BadUrl_SuccessIfException()
        {
            //Arrange
            var extension = ".mp4";
            var file = MockFile(extension);

            //Act
            await Assert.ThrowsAsync<InvalidOperationException>(() => _repository.AddNewPriceProposalAsync(_user, _station, _fuel, 5.0m, file.Object, extension));

            //Assert
            Assert.Empty(_context.PriceProposals.ToList());
            _storage.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _output.WriteLine("Success, AddNewPriceProposalAsync doesn't add a proposal if UploadFileAsync returns a bad url");
        }

        [Fact]
        public async Task GetPriceProposalTest_SuccessIfProposalGet()
        {
            //Arrange
            var photoToken = "token";
            var path = "valid/path.jpg";
            var url = "https://blob.com/valid/path123.jpg";
            var price = 5.0m;
            var createdAt = DateTime.UtcNow;

            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = price,
                Token = photoToken,
                PhotoUrl = path,
                CreatedAt = createdAt,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            _context.PriceProposals.Add(proposal);
            await _context.SaveChangesAsync();

            _storage.Setup(x => x.GetPublicUrl(path, _bucket)).Returns(url).Verifiable();

            //Act
            var result = await _repository.GetPriceProposal(photoToken);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(photoToken, result.Token);
            Assert.Equal(url, result.PhotoUrl);
            Assert.Equal(_brand.Name, result.BrandName);
            Assert.Equal(_address.Street, result.Street);
            _output.WriteLine("Success, GetPriceProposal returns the correct proposal");
        }

        [Fact]
        public async Task GetPriceProposalTest_BadToekn_SuccessIfNullReturned()
        {
            //Arrange
            //-

            //Act
            var result = await _repository.GetPriceProposal(null);

            //Assert
            _storage.Verify(x => x.GetPublicUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            Assert.Null(result);
            _loggerMock.Verify(x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("not found")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
                Times.Once);
            _output.WriteLine("Success, GetPriceProposal returns null when given a bad token");
        }

        [Fact]
        public async Task GetAllPriceProposalTest_SuccessIfPendingProposalsReturned()
        {
            //Arrange
            var proposal1 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal2 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 2.0m,
                Token = "",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Rejected,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal3 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 3.0m,
                Token = "",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal4 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 4.0m,
                Token = "",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Accepted,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            _context.PriceProposals.AddRange(proposal1, proposal2, proposal3, proposal4);
            await _context.SaveChangesAsync();

            var request = new TableRequest
            {
                Search = null,
                SortBy = null,
                SortDirection = null
            };
            //Act
            var result = await _repository.GetAllPriceProposal(request);

            //
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(proposal1.ProposedPrice, result.First().ProposedPrice);
            Assert.Equal(proposal3.ProposedPrice, result.Skip(1).First().ProposedPrice);
            _output.WriteLine("Success, GetAllPriceProposal returns all pending proposals.");
        }

        [Fact]
        public async Task GetAllPriceProposalTest_FilteringAndSearch_SuccessIfPropTwoThenPropOne()
        {
            //Arrange
            var returnUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "testName" };
            var ignoredUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "ignored" };

            _context.Users.AddRange(returnUser, ignoredUser);
            var proposal1 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = returnUser,
                UserId = returnUser.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "1",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal2 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = returnUser,
                UserId = returnUser.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 2.0m,
                Token = "2",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal3 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = ignoredUser,
                UserId = ignoredUser.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 2.0m,
                Token = "3",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            _context.PriceProposals.AddRange(proposal1, proposal2, proposal3);
            await _context.SaveChangesAsync();

            var request = new TableRequest
            {
                Search = "testName",
                SortBy = "createdat",
                SortDirection = "asc"
            };

            //Act
            var result = await _repository.GetAllPriceProposal(request);

            //Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("2", result.First().Token);
            Assert.Equal("1", result.Skip(1).First().Token);
            _output.WriteLine("Success, GetAllPriceProposal sorts and searches correctly");
        }

        [Fact]
        public async Task ChangePriceProposalStatusTest_SuccessIfAccepted()
        {
            //Arrange
            var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin" };
            _context.Users.Add(admin);
            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "1",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };

            _context.PriceProposals.Add(proposal);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.ChangePriceProposalStatus(true, proposal, admin);

            //Assert
            Assert.Equal(PriceProposalStatus.Accepted, _context.PriceProposals.First().Status);
            _output.WriteLine("Success, ChangePriceProposalStatus changes status to accepted.");
        }

        [Fact]
        public async Task ChangePriceProposalStatusTest_SuccessIfRejected()
        {
            //Arrange
            var admin = new ApplicationUser { Id = Guid.NewGuid(), UserName = "admin" };
            _context.Users.Add(admin);
            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "1",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };

            _context.PriceProposals.Add(proposal);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.ChangePriceProposalStatus(false, proposal, admin);

            //Assert
            Assert.Equal(PriceProposalStatus.Rejected, _context.PriceProposals.First().Status);
            _output.WriteLine("Success, ChangePriceProposalStatus changes status to rejected.");
        }

        [Fact]
        public async Task FindPriceProposalTest_SuccessIfProposalFound()
        {
            //Arrange
            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "123",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };

            _context.PriceProposals.Add(proposal);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.FindPriceProposal("123");

            //Assert
            Assert.Equal(proposal.Id, result.Id);
        }

        [Fact]
        public async Task FindPriceProposalTest_BadData_SuccessIfProposalNotFound()
        {
            //Arrange
            var proposal = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "123",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };

            _context.PriceProposals.Add(proposal);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.FindPriceProposal("321");

            //Assert
            Assert.Null(result);
            _output.WriteLine("Success, FindPriceProposal returns null when token is bad");
        }

        [Fact]
        public async Task GetPriceProposalStaisticAsync_SuccessIfNoProposalsReturned()
        {
            //Act
            var result = await _repository.GetPriceProposalStaisticAsync();

            //Assert
            Assert.Equal(0, result.AcceptedRate);
            Assert.Equal(0, result.RejectedRate);
            Assert.Equal(0, result.PendingRate);
            _output.WriteLine("Success, GetPriceProposalStaisticAsync returns 0s when no proposals exist");
        }

        [Fact]
        public async Task GetPriceProposalStaisticAsync_SuccessIfOneOfEachReturned()
        {
            //Arrange
            var proposal1 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "123",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Accepted,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal2 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "123",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Pending,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };
            var proposal3 = new PriceProposal
            {
                Id = Guid.NewGuid(),
                User = _user,
                UserId = _user.Id,
                Station = _station,
                StationId = _station.Id,
                ProposedPrice = 1.0m,
                Token = "123",
                PhotoUrl = "",
                CreatedAt = DateTime.UtcNow,
                Status = PriceProposalStatus.Rejected,
                FuelType = _fuel,
                FuelTypeId = _fuel.Id,
            };

            _context.PriceProposals.AddRange(proposal1, proposal2, proposal3);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetPriceProposalStaisticAsync();

            //Assert
            Assert.Equal(1, result.AcceptedRate);
            Assert.Equal(1, result.RejectedRate);
            Assert.Equal(1, result.PendingRate);
            _output.WriteLine("Success, GetPriceProposalStaisticAsync returns existing proposal rates");
        }
    }

}