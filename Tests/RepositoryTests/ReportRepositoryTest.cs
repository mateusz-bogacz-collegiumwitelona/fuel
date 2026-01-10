using Data.Context;
using Data.Enums;
using Data.Models;
using Data.Reopsitories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class ReportRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ReportRepositry>> _loggerMock;
        private readonly ReportRepositry _repository;
        private readonly ITestOutputHelper _output;

        public ReportRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<ReportRepositry>>();
            _repository = new ReportRepositry(_context, _loggerMock.Object);
        }

        [Fact]
        public async Task ReportUserAsyncTest_SuccessIfReportAddedAndReturnedTrue()
        {
            //Arrange
            var reason = "TestReport";
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting@test.com",
                UserName = "ReportingUser"
            };

            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.com",
                UserName = "ReportedUser"
            };

            //Act
            var result = await _repository.ReportUserAsync(user2, user1, reason);

            //Assert
            var report = await _context.ReportUserRecords.FirstOrDefaultAsync();
            Assert.True(result);
            Assert.Equal("TestReport", report?.Description);
            Assert.Equal(user1.Id, report?.ReportingUserId);
            Assert.Equal(user2.Id, report?.ReportedUserId);
            _output.WriteLine("Success, ReportUserAsync successfuly creates a report.");
        }

        [Fact]
        public async Task GetUserReportAsyncTest_SuccessIfOnlyPendingReportReturned()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting@test.com",
                UserName = "ReportingUser"
            };

            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.com",
                UserName = "ReportedUser"
            };

            var reportAccept = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ReportedUserId = user2.Id,
                ReportingUserId = user1.Id,
                Status = ReportStatusEnum.Accepted,
                Description = "reasonAccept"
            };

            var reportPending = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                ReportedUserId = user2.Id,
                ReportingUserId = user1.Id,
                Status = ReportStatusEnum.Pending,
                Description = "reasonPending"
            };
            _context.Users.AddRange(user1, user2);
            _context.ReportUserRecords.AddRange(reportAccept, reportPending);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetUserReportAsync(user2.Id);

            //Assert
            Assert.Single(result);
            Assert.Equal("ReportingUser", result.First().ReportingUserName);
            Assert.Equal("reasonPending", result.First().Reason);
            Assert.Equal(reportPending.Status.ToString(), result.First().Status);
            _output.WriteLine("Success, GetUserReportAsync returns only the pending reports");
        }

        [Fact]
        public async Task ChangeRepostStatusToAcceptedAsyncTest_SuccessIfStatusChanged()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting@test.com",
                UserName = "ReportingUser"
            };

            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.com",
                UserName = "ReportedUser"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "Admin"
            };

            var ReportAccept = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportedUserId = user2.Id,
                ReportingUserId = user1.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = ReportStatusEnum.Pending,
                Description = "Test"
            };
            _context.Users.AddRange(user1, user2, admin);
            _context.ReportUserRecords.Add(ReportAccept);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.ChangeRepostStatusToAcceptedAsync(user2.Id, user1.Id, admin, ReportAccept.CreatedAt);

            //Assert
            var reportAcceptUpdate = await _context.ReportUserRecords.FindAsync(ReportAccept.Id);
            Assert.True(result);
            Assert.Equal(ReportStatusEnum.Accepted, reportAcceptUpdate?.Status);
            Assert.Equal(ReportAccept.Id, reportAcceptUpdate?.Id);
            _output.WriteLine("Success, ChangeRepostStatusToAcceptedAsync changes the pending status to accepted.");
        }

        [Fact]
        public async Task ChangeRepostStatusToAcceptedAsyncTest_ReportNotFound_SuccessIfReturnsFalse()
        {
            //Arrange
            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "admin",
                Email = "admin@test.com"
            };
            
            //Act
            var result = await _repository.ChangeRepostStatusToAcceptedAsync(Guid.Empty, Guid.Empty, admin, DateTime.UtcNow);

            //Assert
            Assert.False(result);
            _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Report not found")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
            _output.WriteLine("Succes, ChangeRepostStatusToAcceptedAsync returns and throws an error when given bad data");
        }

        [Fact]
        public async Task ChangeRepostStatusToRejectAsyncTest_SuccessIfRejected()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting@test.com",
                UserName = "ReportingUser"
            };

            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported@test.com",
                UserName = "ReportedUser"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "Admin"
            };

            var ReportReject = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportedUserId = user2.Id,
                ReportingUserId = user1.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                Status = ReportStatusEnum.Pending,
                Description = "Test"
            };
            _context.Users.AddRange(user1, user2, admin);
            _context.ReportUserRecords.Add(ReportReject);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.ChangeRepostStatusToRejectAsync(user2.Id, user1.Id, admin, ReportReject.CreatedAt);

            //Assert
            var reportRejectUpdate = await _context.ReportUserRecords.FindAsync(ReportReject.Id);
            Assert.True(result);
            Assert.Equal(ReportStatusEnum.Rejected, reportRejectUpdate?.Status);
            Assert.Equal(ReportReject.Id, reportRejectUpdate?.Id);
            _output.WriteLine("Success, ChangeRepostStatusToRejectedAsync changes the pending status to rejected.");
        }

        [Fact]
        public async Task ChangeRepostStatusToRejectAsyncTest_ReportNotFound_SuccessIfReturnsFalse()
        {
            //Arrange
            var admin = new ApplicationUser
            {
                UserName = "admin",
                Email = "admin@test.com",
                Id = Guid.NewGuid(),
            };

            //Act
            var result = await _repository.ChangeRepostStatusToRejectAsync(Guid.Empty, Guid.Empty, admin, DateTime.UtcNow);

            //Assert
            Assert.False(result);
            _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Report not found")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
            _output.WriteLine("Succes, ChangeRepostStatusToRejectAsync returns and throws an error when given bad data");
        }

        [Fact]
        public async Task ClearReports_SuccessIfCleared()
        {
            //Arrange
            var reporting1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting1@test.com",
                UserName = "Reporting1"
            };
            var reporting2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reporting2@test.com",
                UserName = "Reporting2"
            };

            var reported1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported1@test.com",
                UserName = "Reported1"
            };

            var reported2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "reported2@test.com",
                UserName = "Reported2"
            };

            var admin = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "Admin"
            };

            var pending1 = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportingUserId = reporting1.Id,
                ReportedUserId = reported1.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-10),
                Status = ReportStatusEnum.Pending,
                Description = "report1"
            };

            var pending2 = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportingUserId = reporting2.Id,
                ReportedUserId = reported2.Id,
                CreatedAt = DateTime.UtcNow.AddHours(-5),
                Status = ReportStatusEnum.Pending,
                Description = "report2"
            };

            _context.Users.AddRange(reporting1, reporting2, reported1, reported2, admin);
            _context.ReportUserRecords.AddRange(pending1, pending2);
            await _context.SaveChangesAsync();

            // Act
            await _repository.ClearReports(reported1.Id, admin);
            await _repository.ClearReports(reported2.Id, admin);

            // Assert
            var updated1 = await _context.ReportUserRecords.FindAsync(pending1.Id);
            var updated2 = await _context.ReportUserRecords.FindAsync(pending2.Id);

            Assert.Equal(ReportStatusEnum.Accepted, updated1?.Status);
            Assert.Equal(ReportStatusEnum.Accepted, updated2?.Status);
            Assert.Equal(admin.Id, updated1?.ReviewedByAdminId);
            Assert.Equal(admin.Id, updated2?.ReviewedByAdminId);
            Assert.NotNull(updated1?.ReviewedAt);
            Assert.NotNull(updated2?.ReviewedAt);

            _output.WriteLine("Success, ClearReports marks all pending reportsas accepted");
        }
    }
}
