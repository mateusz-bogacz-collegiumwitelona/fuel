using Data.Context;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using Microsoft.EntityFrameworkCore;
using Xunit.Abstractions;

namespace Tests.RepositoryTests
{
    public class BanRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly BanRepository _repository;
        private readonly ITestOutputHelper _output;
        public BanRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);
            _repository = new BanRepository(_context);
        }

        [Fact]
        public async Task AddBanRecordAsync_SuccessIfBanRecordAdded()
        {
            //Arrange
            var user = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "banned@test.com",
                UserName = "BannedUser"
            };
            var admin = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "admin"
            };
            var request = new SetLockoutForUserRequest
            {
                Reason = "Test ban",
                Days = 1
            };

            //Act
            var result = await _repository.AddBanRecordAsync(user, admin, request);

            //Assert
            Assert.True(result);
            Assert.Equal(1,_context.BanRecords.Count());
            var bannedUser = _context.BanRecords.First();
            Assert.Equal(user.Id, bannedUser.UserId);
            Assert.Equal(admin.Id, bannedUser.AdminId);
            Assert.Equal("Test ban", bannedUser.Reason);
            Assert.True(bannedUser.IsActive);
            Assert.NotNull(bannedUser.BannedUntil);
            _output.WriteLine("AddBanRecordAsync correctly bans the user");
        }

        [Fact]
        public async Task DeactivateActiveBansAsyncTest_SuccessIfBansDeleted()
        {
            //Arrange
            var user = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "banned@test.com",
                UserName = "BannedUser"
            };
            var admin = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "admin"
            };
            var request = new SetLockoutForUserRequest
            {
                Reason = "Test ban",
                Days = 1
            };
            await _repository.AddBanRecordAsync(user, admin, request);

            //Act
            var result = _repository.DeactivateActiveBansAsync(user.Id, admin.Id);

            //Assert
            var bans = _context.BanRecords.ToList();

            Assert.All(bans, b => Assert.False(b.IsActive));
            Assert.All(bans, b => Assert.NotNull(b.UnbannedAt));
            Assert.All(bans, b => Assert.Equal(admin.Id, b.UnbannedByAdminId));
            _output.WriteLine("DeactivateActiveBansAsync correctly unbans the user");
        }

        [Fact]
        public async Task GetUserBanInfoAsyncTest_SuccessIfCorrectInfoReturned()
        {
            //Arrange
            var user = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "banned@test.com",
                UserName = "BannedUser"
            };
            var admin = new ApplicationUser()
            {
                Id = Guid.NewGuid(),
                Email = "admin@test.com",
                UserName = "admin"
            };
            var request = new SetLockoutForUserRequest
            {
                Reason = "Test ban",
                Days = 1
            };
            await _repository.AddBanRecordAsync(user, admin, request);

            //Act
            var result = await _repository.GetUserBanInfoAsync(user.Email);

            //Assert
            Assert.NotNull(result);
            Assert.Equal(result.UserName, user.UserName);
            Assert.Equal(result.Reason, request.Reason);
            Assert.Equal(result.BannedBy, admin.UserName);
            _output.WriteLine("GetUserBanInfoAsync returns correct ban record");
        }
    }
}
