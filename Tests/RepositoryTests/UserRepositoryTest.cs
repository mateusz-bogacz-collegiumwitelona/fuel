using Data.Context;
using Data.Helpers;
using Data.Models;
using Data.Reopsitories;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
namespace Tests.RepositoryTests
{
    public class UserRepositoryTest
    {
        private readonly ApplicationDbContext _context;
        private readonly Mock<ILogger<UserRepository>> _loggerMock;
        private readonly UserRepository _repository;
        private readonly ITestOutputHelper _output;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;

        public UserRepositoryTest(ITestOutputHelper output)
        {
            _output = output;
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning)).Options;
            _context = new ApplicationDbContext(options);
            _loggerMock = new Mock<ILogger<UserRepository>>();
            var _userMock = new Mock<IUserStore<ApplicationUser>>();
            _userManagerMock = new Mock<UserManager<ApplicationUser>>(_userMock.Object, null, null, null, null, null, null, null, null);
            _repository = new UserRepository(_context, _loggerMock.Object, _userManagerMock.Object);
        }

        [Fact]
        public async Task IsUserDeletedTest_SuccessIfReturnsTrueAndFalse()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                UserName = "user1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com",
                UserName = "user2",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = true,
                DeletdAt = DateTime.UtcNow.AddHours(-1)
            };

            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();

            //Act
            var resultFalse = await _repository.IsUserDeleted(user1);
            var resultTrue = await _repository.IsUserDeleted(user2);

            //Assert
            Assert.True(resultTrue);
            Assert.False(resultFalse);
            _output.WriteLine("Success, IsUserDeleted correctly checks the deleted status of users");
        }

        [Fact]
        public async Task DeleteUserAsyncTest_SuccessIfUserDeleted()
        {
            //Arrange
            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                UserName = "user1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            _context.Users.Add(user1);
            await _context.SaveChangesAsync();

            //Act
            var result = await _repository.DeleteUserAsync(user1);

            //Assert
            var userUpdate = await _context.Users.FirstAsync();
            Assert.True(result.Succeeded);
            Assert.Equal(user1.Id, userUpdate.Id);
            Assert.True(userUpdate.IsDeleted);
            Assert.NotNull(userUpdate.DeletdAt);
            _output.WriteLine("Success, DeleteUserAsync changes flags in a deleted user correctly");
        }

        [Fact]
        public async Task DeleteUserAsyncTest_BadData_SuccessIfLogWarning()
        {
            //Arrange
            var badUser = new ApplicationUser { Id = Guid.NewGuid() };

            //Act
            var result = await _repository.DeleteUserAsync(badUser);

            //Assert
            Assert.False(result.Succeeded);
            _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("not found for deletion")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception, string>>((v, t) => true)),
            Times.Once);
            _output.WriteLine("Success, DeleteUserAsync returns an error when trying to delete a non existent user");
        }

        [Fact]
        public async Task GetUserListAsyncTest_SuccessIfListReturnedWithNoDeletedUsers()
        {
            //Arrange
            var role = new IdentityRole<Guid>("user");
            _context.Roles.Add(role);

            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                UserName = "user1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com",
                UserName = "user2",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = true,
                DeletdAt = DateTime.UtcNow.AddHours(-1)
            };
            _context.UserRoles.AddRange(new IdentityUserRole<Guid> {
                UserId = user1.Id, 
                RoleId =  role.Id
            },
                new IdentityUserRole<Guid>
                {
                UserId = user2.Id,
                RoleId = role.Id
                });
            _context.Users.AddRange(user1, user2);
            await _context.SaveChangesAsync();
            var request = new TableRequest
            {
                Search = "",
                SortBy = "Username",
                SortDirection = "asc"
            };

            //Act
            var result = await _repository.GetUserListAsync(request);

            //Assert
            Assert.NotEmpty(result);
            Assert.Equal(1, result.ToList().Count());
            Assert.Equal(user1.UserName, result.ToList().First().UserName);
            _output.WriteLine("Success, GetUserListAsync returns a list of users excluding deleted ones,.");
        }

        [Fact]
        public async Task GetUserListAsyncTest_SuccessIfRecognizesBanStatus()
        {
            //Arrange
            var role = new IdentityRole<Guid>("user");
            _context.Roles.Add(role);

            var user1 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user1@test.com",
                UserName = "user1",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            var user2 = new ApplicationUser
            {
                Id = Guid.NewGuid(),
                Email = "user2@test.com",
                UserName = "user2",
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                IsDeleted = false
            };
            _context.UserRoles.AddRange(new IdentityUserRole<Guid>
            {
                UserId = user1.Id,
                RoleId = role.Id
            },
                new IdentityUserRole<Guid>
                {
                    UserId = user2.Id,
                    RoleId = role.Id
                });
            _context.Users.AddRange(user1, user2);
            _context.BanRecords.Add(new BanRecord{ 
                UserId = user1.Id, 
                IsActive = false, 
                BannedAt = DateTime.UtcNow.AddHours(-1),
                Reason = "Test"
            });
            _context.BanRecords.Add(new BanRecord
            {
                UserId = user2.Id,
                IsActive = true,
                BannedAt = DateTime.UtcNow.AddHours(-1),
                Reason = "Test"
            });

            await _context.SaveChangesAsync();

            var request = new TableRequest
            {
                Search = "",
                SortBy = "Username",
                SortDirection = "asc"
            };

            //Act
            var result = await _repository.GetUserListAsync(request);

            //Assert
            Assert.NotEmpty(result);
            Assert.Equal(2, result.Count());
            Assert.Equal(user1.UserName, result.ToList().First().UserName);
            Assert.False(result.ToList().First().IsBanned);
            Assert.Equal(user2.UserName, result.ToList().Skip(1).First().UserName);
            Assert.True(result.ToList().Skip(1).First().IsBanned);
            _output.WriteLine("Success, GetUserListAsync sees active ban status");
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
            Assert.Equal("TestReport", report.Description);
            Assert.Equal(user1.Id, report.ReportingUserId);
            Assert.Equal(user2.Id, report.ReportedUserId);
            _output.WriteLine("Success, ReportUserAsync successfuly creates a report.");
        }
    }
}
