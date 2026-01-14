using Data.Context;
using Data.Models;
using Data.Reopsitories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class ProposalStatisticRepositoryTests
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ProposalStatisticRepository>> _loggerMock;
        private readonly ProposalStatisticRepository _repository;
        private readonly ITestOutputHelper _output;

        public ProposalStatisticRepositoryTests(ITestOutputHelper output)
        {
            _output = output;

            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new ApplicationDbContext(options);

            _loggerMock = new Mock<ILogger<ProposalStatisticRepository>>();

            _repository = new ProposalStatisticRepository(_context, null, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUserProposalStatisticAsyncTest_NoStats_SuccessIfReturnsNull()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };

            //Act
            var result = await _repository.GetUserProposalStatisticAsync(user);

            //Assert
            Assert.Null(result);
            _loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("user with email")),
                    It.Is<Exception>(ex => ex == null),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            _output.WriteLine("Success, GetUserProposalStatisticAsync returns null when no stats exist");
        }

        [Fact]
        public async Task GetUserProposalStatisticAsyncTest_StatsExist_SuccessIfTheyGetReturned()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };

            _context.ProposalStatistics.Add(new ProposalStatistic
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TotalProposals = 10,
                ApprovedProposals = 3,
                RejectedProposals = 7,
                AcceptedRate = 30,
                Points = 3,
                UpdatedAt = DateTime.UtcNow,
            });
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetUserProposalStatisticAsync(user);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalProposals);
            Assert.Equal(3, result.ApprovedProposals);
            Assert.Equal(7, result.RejectedProposals);
            Assert.Equal(30, result.AcceptedRate);
            Assert.Equal(3, result.Points);
            _output.WriteLine("Success, GetUserProposalStatisticAsync returns correct statistics");
        }

        [Fact]
        public async Task AddProposalStatisticRecordAsyncTest_SuccessIfRecordAdded()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };

            //Act
            var result = await _repository.AddProposalStatisticRecordAsync(user);

            //Assert
            Assert.True(result);
            Assert.Equal(1, _context.ProposalStatistics.Count());

            var addedStat = _context.ProposalStatistics.First();
            Assert.Equal(user.Id, addedStat.UserId);
            Assert.Equal(0, addedStat.TotalProposals);
            _output.WriteLine("AddProposalStatisticRecord correctly adds a record.");
        }

        [Fact]
        public async Task UpdateTotalProposalsAsyncTest_NoStatsFound_SuccessIfReturnedFalse()
        {
            //Arrange
            //---

            //Act
            var result = await _repository.UpdateTotalProposalsAsync(true, Guid.NewGuid());

            //Assert
            Assert.False(result);
            _loggerMock.Verify(x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No proposal statistics found for user")),
                    It.Is<Exception>(ex => ex == null),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
            _output.WriteLine("UpdateTotalProposalsAsync returns false if user has no stats.");
        }

        [Fact]
        public async Task UpdateTotalProposalsAsyncTest_Accepted_SuccessIfAcceptedIncreased()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };
            await _repository.AddProposalStatisticRecordAsync(user);

            //Act
            var result = await _repository.UpdateTotalProposalsAsync(true, user.Id);

            //Assert
            Assert.True(result);
            var userStats = await _repository.GetUserProposalStatisticAsync(user);
            Assert.Equal(1, userStats.TotalProposals);
            Assert.Equal(1, userStats.ApprovedProposals);
            Assert.Equal(0, userStats.RejectedProposals);
            Assert.Equal(100, userStats.AcceptedRate);
            _output.WriteLine("UpdateTotalProposalsAsync updates accepted, total and acceptance rate");
        }

        [Fact]
        public async Task UpdateTotalProposalsAsyncTest_Rejected_SuccessIfRejectedIncreased()
        {
            //Arrange
            var user = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user@test.com"
            };
            await _repository.AddProposalStatisticRecordAsync(user);

            //Act
            var result = await _repository.UpdateTotalProposalsAsync(false, user.Id);

            //Assert
            Assert.True(result);
            var userStats = await _repository.GetUserProposalStatisticAsync(user);
            Assert.Equal(1, userStats.TotalProposals);
            Assert.Equal(1, userStats.RejectedProposals);
            Assert.Equal(0, userStats.ApprovedProposals);
            Assert.Equal(0, userStats.AcceptedRate);
            _output.WriteLine("UpdateTotalProposalsAsync updates rejected, total and acceptance rate");
        }

        [Fact]
        public async Task GetTopUserListAsyncTest_SuccessWhenOrderedUsersReturned()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user1"
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                UserName = "user2"
            };
            _context.ProposalStatistics.AddRange(
                new ProposalStatistic
                {
                    Id = Guid.NewGuid(),
                    UserId = user1.Id,
                    User = user1,
                    Points = 1
                },
                new ProposalStatistic
                {
                    Id = Guid.NewGuid(),
                    UserId = user2.Id,
                    User = user2,
                    Points = 10
                });
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.GetTopUserListAsync();

            //Assert
            Assert.Equal(2, result.Count());
            Assert.Equal("user2", result.First().UserName);
            Assert.Equal("user1", result[1].UserName);
            _output.WriteLine("GetTopUserListAsync returns all users in the correct order.");
        }
    }
}
