using Data.Context;
using Data.Models;
using Data.Reopsitories;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit.Abstractions;

namespace Tests.RepositoryTest
{
    public class ProposalStatisticRepositoryTests
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<ProposalStatisticRepository>> _loggerMock;
        private readonly ProposalStatisticRepository _repository;
        private readonly ITestOutputHelper _output;

        public ProposalStatisticRepositoryTests(ITestOutputHelper output)
        {
            // test output setup
            _output = output;

            // inMemory db setup
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // mock um setup
            var store = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(
                store.Object, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null, 
                null
            );

            _loggerMock = new Mock<ILogger<ProposalStatisticRepository>>();
            
            // repo setup
            _repository = new ProposalStatisticRepository(_context, _userManagerMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task GetUserProposalStatisticAsyncTest_ExistingUser_SuccessWhenUserStatisticsMatch()
        {
            // Arrange
            // create user, connect user to FindByEmailAsync
            var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);

            // statistics for new user
            var stat = new ProposalStatistic
            {
                Id = Guid.NewGuid(),
                UserId = newUser.Id,
                TotalProposals = 10,
                ApprovedProposals = 6,
                RejectedProposals = 4,
                AcceptedRate = 60,
                UpdatedAt = DateTime.UtcNow
            };

            // adds statistics && saves changes
            _context.ProposalStatistics.Add(stat);
            await _context.SaveChangesAsync();

            // Act
            // gets user statistics based on email
            var result = await _repository.GetUserProposalStatisticAsync(newUser);

            // Assert
            // checks if the values match with expectations
            Assert.NotNull(result);
            Assert.Equal(10, result.TotalProposals);
            Assert.Equal(6, result.ApprovedProposals);
            Assert.Equal(4, result.RejectedProposals);
            Assert.Equal(60, result.AcceptedRate);
            _output.WriteLine("Test passed: GetUserProposalStatisticAsync() is working correctly");
        }

        //[Fact]
        //public async Task GetUserProposalStatisticAsyncTest_NonexistantUser_SuccessWhenReturnedNull()
        //{
        //    // Arrange
        //    //

        //    //Act
        //    var result = await _repository.GetUserProposalStatisticAsync("notfound@example.com");

        //    // Assert
        //    Assert.Null(result);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: GetUserProposalStatisticAsync() doesn't let a nonexistent email address pass");
        //}

        //[Fact]
        //public async Task GetUserProposalStatisticAsyncTest_ExistingUserNoStats_SuccessWhenReturnedNull()
        //{
        //    // Arrange
        //    var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
        //    _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);

        //    //Act
        //    var result = await _repository.GetUserProposalStatisticAsync("user123@example.com");

        //    // Assert
        //    Assert.Null(result);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No proposal statistics found for user with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: GetUserProposalStatisticAsync() doesn't let a nonexistent email address pass");
        //}

        //[Fact]
        //public async Task AddProposalStatisticRecordAsuncTest_NonexistantUser_SuccessIfReturnsFalse()
        //{
        //    // Arrange
        //    // everything needed has been mocked and set up already

        //    // Act
        //    // checks nonexisting email, function should return false in this case
        //    var result = await _repository.AddProposalStatisticRecordAsync("notfound@example.com");

        //    // Assert
        //    // if we got a false response, function works fine
        //    Assert.False(result);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: AddProposalStatisticRecordAsunc() doesn't let a nonexistent email address pass");
        //}

        [Fact]
        public async Task AddProposalStatisticRecordAsunc_SuccessIfChangesSaved()
        {
            // Arrange
            // creating a new user and hooking him up to an email
            var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
            _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);

            // Act
            var result = await _repository.AddProposalStatisticRecordAsync(newUser);
            var result2 = await _repository.AddProposalStatisticRecordAsync(newUser);

            // Assert
            // should ALWAYS pass and return true due to how AddProposalStatisticRecordAsunc() and SaveChangesAsync() work,
            // (returns value >0 if changes were made => AddProposalStatisticRecordAsunc() returns true;
            // returns ==0 if no change has been made => AddProposalStatisticRecordAsunc() returns false but
            // AddProposalStatisticRecordAsunc() makes at least one change every time its called (creates new ProposalStatistic.Id);
            // returns <0 if something went completely wrong)
            Assert.True(result);
            Assert.True(result2);
            _output.WriteLine("Test passed: AddProposalStatisticRecordAsunc() saves its changes");
        }

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_NonexistentUser_SuccessIfReturnedFalse()
        //{
        //    // Arrange
        //    // not creating an user
        //    // Act
        //    var result = await _repository.UpdateTotalProposalsAsync(true, "user123@example.com");

        //    //Assert
        //    Assert.False(result);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("User with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() doesn't let a nonexistent email address pass");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_UserExistsButHasNoStats_SuccessIfReturnedFalse()
        //{
        //    // Arrange
        //    // creating a user with no data
        //    var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
        //    _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);

        //    // Act
        //    var result = await _repository.UpdateTotalProposalsAsync(true, "user123@example.com");

        //    //Assert
        //    Assert.False(result);
        //    _loggerMock.Verify(x => x.Log(
        //        LogLevel.Warning,
        //        It.IsAny<EventId>(),
        //        It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("No proposal statistics found for user with email")),
        //        It.Is<Exception>(ex => ex == null),
        //        It.IsAny<Func<It.IsAnyType, Exception, string>>()), Times.Once);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() finds the user but he has no stats");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_UserExistsAndHasStats_SuccessIfApprovedIncreased()
        //{
        //    // Arrange
        //    // creating user, hooking him to email, and adding statistics
        //    var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
        //    _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);
        //    var stat = new ProposalStatistic
        //    {
        //        Id = Guid.NewGuid(),
        //        UserId = newUser.Id,
        //        TotalProposals = 2,
        //        ApprovedProposals = 1,
        //        RejectedProposals = 1,
        //        AcceptedRate = 50,
        //        UpdatedAt = DateTime.UtcNow
        //    };
        //    _context.ProposalStatistics.Add(stat);
        //    await _context.SaveChangesAsync();

        //    // Act
        //    // approving a proposal and fetching user stats
        //    var setUp = await _repository.UpdateTotalProposalsAsync(true, "user123@example.com");
        //    var result = await _repository.GetUserProposalStatisticAsync(newUser);

        //    // Assert
        //    Assert.Equal(3, result.TotalProposals);
        //    Assert.Equal(2, result.ApprovedProposals);
        //    Assert.Equal(1, result.RejectedProposals);
        //    Assert.Equal(66, result.AcceptedRate);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() updates an existing user's stats (approved++)");
        //}
        
        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_UserExistsAndHasStats_SuccessIfRejectedIncreased()
        //{
        //    // Arrange
        //    // creating user, hooking him to email, and adding statistics
        //    var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };
        //    _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email)).ReturnsAsync(newUser);
        //    var stat = new ProposalStatistic
        //    {
        //        Id = Guid.NewGuid(),
        //        UserId = newUser.Id,
        //        TotalProposals = 2,
        //        ApprovedProposals = 1,
        //        RejectedProposals = 1,
        //        AcceptedRate = 50,
        //        UpdatedAt = DateTime.UtcNow
        //    };
        //    _context.ProposalStatistics.Add(stat);
        //    await _context.SaveChangesAsync();

        //    // Act
        //    // rejecting an approval and fetching user stats
        //    var setUp = await _repository.UpdateTotalProposalsAsync(false, "user123@example.com");
        //    var result = await _repository.GetUserProposalStatisticAsync(newUser);

        //    //Assert
        //    Assert.Equal(3, result.TotalProposals);
        //    Assert.Equal(1, result.ApprovedProposals);
        //    Assert.Equal(2, result.RejectedProposals);
        //    Assert.Equal(33, result.AcceptedRate);
        //    _output.WriteLine("Test passed: UpdateTotalProposalsAsync() updates an existing user's stats (rejected++)");
        //}

        //[Fact]
        //public async Task UpdateTotalProposalsAsyncTest_ThrowingException_SuccessIfReturnedFalseAndErrorLogged()
        //{
        //    // Arrange
        //    var newUser = new ApplicationUser { Id = new Guid(), Email = "user123@example.com" };

        //    // um should throw an exception for this email
        //    _userManagerMock.Setup(x => x.FindByEmailAsync(newUser.Email))
        //                    .ThrowsAsync(new Exception("Fake Exception"));

        //    // Act
        //    var result = await _repository.UpdateTotalProposalsAsync(true, newUser.Email);

        //    // Assert
        //    Assert.False(result);
        //    _loggerMock.Verify(
        //        x => x.Log(
        //            LogLevel.Error,
        //            It.IsAny<EventId>(),
        //            It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("An error occurred while updating proposal statistics")),
        //            It.Is<Exception>(ex => ex.Message == "Fake Exception"),
        //            It.IsAny<Func<It.IsAnyType, Exception, string>>()),
        //        Times.Once);
        //}
    }
}
