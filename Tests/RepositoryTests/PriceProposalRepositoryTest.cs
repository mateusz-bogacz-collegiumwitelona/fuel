using Data.Context;
using Data.Enums;
using Data.Helpers;
using Data.Interfaces;
using Data.Models;
using Data.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Minio;
using Moq;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using System.Text;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class PriceProposalRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly PriceProposalRepository _repository;
        private readonly Mock<ILogger<PriceProposalRepository>> _loggerMock;
        private readonly Mock<IS3ApiHelper> _s3ApiHelperMock;
        private readonly Mock<IConfiguration> _configMock;
        private readonly ITestOutputHelper _output;
        private readonly Mock<IMinioClient> _minioMock;
        private readonly Mock<ILogger<S3ApiHelper>> _helperLogMock;

        private readonly string _bucket = "proposal";
        private readonly Brand _brand = new() { Id= Guid.NewGuid(), Name = "Brand"};
        private readonly FuelType _fuel = new() { Id = Guid.NewGuid(), Code = "ON", Name = "Diesel"};
        private readonly StationAddress _address;
        private readonly ApplicationUser _user = new() { Id = Guid.NewGuid(), Email = "user@test.com" };
        private readonly Station _station;

        public PriceProposalRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var geometryFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326);
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;

            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<PriceProposalRepository>>();
            _configMock = new Mock<IConfiguration>();
            _minioMock = new Mock<IMinioClient>();
            _helperLogMock = new Mock<ILogger<S3ApiHelper>>();
            _s3ApiHelperMock = new Mock<IS3ApiHelper>(MockBehavior.Strict);

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

            _repository = new PriceProposalRepository(_context, _loggerMock.Object, _s3ApiHelperMock.Object, _configMock.Object);

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
            var url = "http://minio/test.jpg";
            _s3ApiHelperMock.Setup(x => x.UploadFileAsync(
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
            _s3ApiHelperMock.Verify(x => x.UploadFileAsync(
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
            var url = "http://minio/test.jpg";
            _s3ApiHelperMock.Setup(x => x.UploadFileAsync(
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
            _s3ApiHelperMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
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
            _s3ApiHelperMock.Verify(x => x.UploadFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never());
            _output.WriteLine("Success, AddNewPriceProposalAsync doesn't add a proposal if UploadFileAsync returns a bad url");
        }

        [Fact]
        public async Task GetPriceProposalTest_SuccessIfProposalGet()
        {
            //Arrange
            var photoToken = "token";
            var path = "valid/path.jpg";
            var url = "https://minio.com/valid/path123.jpg";
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

            _s3ApiHelperMock.Setup(x => x.GetPublicUrl(path, _bucket)).Returns(url).Verifiable();

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
            _s3ApiHelperMock.Verify(x => x.GetPublicUrl(It.IsAny<string>(), It.IsAny<string>()), Times.Never());
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
    }
}