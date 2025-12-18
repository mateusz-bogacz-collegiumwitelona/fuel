using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Helpers;
using Services.Services;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Xunit;

namespace Tests.ServicesTests
{
    public class UserServicesTest
    {
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<ILogger<UserServices>> _loggerMock;
        private readonly Mock<EmailSender> _emailMock;
        private readonly CacheService _cache;
        private readonly UserServices _service;

        public UserServicesTest()
        {
            _userRepositoryMock = new Mock<IUserRepository>();
            _userManagerMock = CreateUserManagerMock();
            _roleManagerMock = CreateRoleManagerMock();
            _loggerMock = new Mock<ILogger<UserServices>>();
            _emailMock = CreateEmailSenderMock();
            _cache = CreateCacheServiceMock();

            _service = new UserServices(
                _userRepositoryMock.Object,
                _loggerMock.Object,
                _userManagerMock.Object,
                _roleManagerMock.Object,
                _emailMock.Object,
                _cache
            );
        }

      

        [Fact]
        public async Task ChangeUserNameAsync_EmailIsNull_Returns401()
        {
            // Arrange
            string email = string.Empty;
            string newUserName = "newname";

            // Act
            var result = await _service.ChangeUserNameAsync(email, newUserName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserNameAsync_UserNotFound_Returns404()
        {
            // Arrange
            string email = "notfound@example.com";
            string newUserName = "newname";
            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.ChangeUserNameAsync(email, newUserName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserNameAsync_UserNameAlreadyExists_Returns409()
        {
            // Arrange
            string email = "user@example.com";
            string newUserName = "existingname";
            ApplicationUser existingUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "user2@example.com", UserName = newUserName };
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = "oldname" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.FindByNameAsync(newUserName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _service.ChangeUserNameAsync(email, newUserName);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status409Conflict, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserNameAsync_Success_Returns200()
        {
            // Arrange
            string email = "user@example.com";
            string newUserName = "newname";
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email, UserName = "oldname" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.FindByNameAsync(newUserName))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.SetUserNameAsync(user, newUserName))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.UpdateAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.ChangeUserNameAsync(email, newUserName);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

      


        [Fact]
        public async Task ChangeUserPasswordAsync_PasswordsDoNotMatch_Returns400()
        {
            // Arrange
            string email = "user@example.com";
            ChangePasswordRequest request = new ChangePasswordRequest
            {
                CurrentPassword = "current",
                NewPassword = "new1",
                ConfirmNewPassword = "new2"
            };

            // Act
            var result = await _service.ChangeUserPasswordAsync(email, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserPasswordAsync_IncorrectCurrentPassword_Returns400()
        {
            // Arrange
            string email = "user@example.com";
            ChangePasswordRequest request = new ChangePasswordRequest
            {
                CurrentPassword = "wrong",
                NewPassword = "NewPass123!",
                ConfirmNewPassword = "NewPass123!"
            };
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, request.CurrentPassword))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ChangeUserPasswordAsync(email, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserPasswordAsync_Success_Returns200()
        {
            // Arrange
            string email = "user@example.com";
            ChangePasswordRequest request = new ChangePasswordRequest
            {
                CurrentPassword = "current",
                NewPassword = "NewPass123!",
                ConfirmNewPassword = "NewPass123!"
            };
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, request.CurrentPassword))
                .ReturnsAsync(true);
            _userManagerMock.Setup(um => um.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.ChangeUserPasswordAsync(email, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        


        [Fact]
        public async Task ChangeUserRoleAsync_EmailIsNull_Returns400()
        {
            // Arrange
            string email = string.Empty;
            string newRole = "Admin";

            // Act
            var result = await _service.ChangeUserRoleAsync(email, newRole);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsync_UserNotFound_Returns404()
        {
            // Arrange
            string email = "notfound@example.com";
            string newRole = "Admin";
            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.ChangeUserRoleAsync(email, newRole);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsync_RoleNotFound_Returns404()
        {
            // Arrange
            string email = "user@example.com";
            string newRole = "NonExistingRole";
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(newRole))
                .ReturnsAsync(false);

            // Act
            var result = await _service.ChangeUserRoleAsync(email, newRole);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
        }

        [Fact]
        public async Task ChangeUserRoleAsync_Success_Returns200()
        {
            // Arrange
            string email = "user@example.com";
            string newRole = "Admin";
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };
            List<string> currentRoles = new List<string> { "User" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync(newRole))
                .ReturnsAsync(true);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(currentRoles);
            _userManagerMock.Setup(um => um.RemoveFromRolesAsync(user, currentRoles))
                .ReturnsAsync(IdentityResult.Success);
            _userManagerMock.Setup(um => um.AddToRoleAsync(user, newRole))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.ChangeUserRoleAsync(email, newRole);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }



        [Fact]
        public async Task DeleteUserAsyc_PasswordMismatch_Returns400()
        {
            // Arrange
            string email = "user@example.com";
            DeleteAccountRequest request = new DeleteAccountRequest
            {
                Password = "one",
                ConfirmPassword = "two"
            };

            // Act
            var result = await _service.DeleteUserAsyc(email, request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        }

        [Fact]
        public async Task DeleteUserAsyc_Success_Returns200()
        {
            // Arrange
            string email = "user@example.com";
            DeleteAccountRequest request = new DeleteAccountRequest
            {
                Password = "current",
                ConfirmPassword = "current"
            };
            ApplicationUser user = new ApplicationUser { Id = Guid.NewGuid(), Email = email };

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.CheckPasswordAsync(user, request.Password))
                .ReturnsAsync(true);
            _userRepositoryMock.Setup(ur => ur.DeleteUserAsync(user))
                .ReturnsAsync(IdentityResult.Success);

            // Act
            var result = await _service.DeleteUserAsyc(email, request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
        }

        

        private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            Mock<IUserStore<ApplicationUser>> userStoreMock = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(
                userStoreMock.Object,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!,
                null!
            );
        }

        private Mock<RoleManager<IdentityRole<Guid>>> CreateRoleManagerMock()
        {
            Mock<IRoleStore<IdentityRole<Guid>>> roleStoreMock = new Mock<IRoleStore<IdentityRole<Guid>>>();
            return new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStoreMock.Object,
                null!,
                null!,
                null!,
                null!
            );
        }

        private Mock<EmailSender> CreateEmailSenderMock()
        {
            var inMemorySettings = new Dictionary<string, string?>
            {
                ["Frontend:Url"] = "http://localhost:4000",
                // ustawienia mail pozostaw puste/bezpieczne, aby SendEmailAsync nie próbował wysyłać prawdziwej poczty
                ["Mail:Host"] = "",
                ["Mail:Port"] = "1025",
                ["Mail:EnableSsl"] = "false",
                ["Mail:From"] = ""
            };

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();

            var emailBodys = new EmailBodys();

            return new Mock<EmailSender>(
                Mock.Of<ILogger<EmailSender>>(),
                configuration,
                emailBodys
            );
        }

        private CacheService CreateCacheServiceMock()
        {
            var connMock = new Mock<IConnectionMultiplexer>();
            var dbMock = new Mock<IDatabase>();
            var serverMock = new Mock<IServer>();

            // DB safe defaults
            dbMock.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(RedisValue.Null);
            dbMock.Setup(d => d.StringSetAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<TimeSpan?>(), It.IsAny<When>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(true);
            dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey[]>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(0L);
            dbMock.Setup(d => d.KeyDeleteAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
                .ReturnsAsync(false);

            // Server returns no keys for any pattern
            serverMock.Setup(s => s.Keys(It.IsAny<int>(), It.IsAny<RedisValue>(), It.IsAny<int>(), It.IsAny<long>(), It.IsAny<int>(), It.IsAny<CommandFlags>()))
                .Returns(Enumerable.Empty<RedisKey>());

            // ConnectionMultiplexer wiring
            connMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>()))
                .Returns(dbMock.Object);

            var endpoint = new DnsEndPoint("127.0.0.1", 6379);
            connMock.Setup(r => r.GetEndPoints(It.IsAny<bool>()))
                .Returns(new EndPoint[] { endpoint });
            connMock.Setup(r => r.GetServer(It.IsAny<EndPoint>(), It.IsAny<object>()))
                .Returns(serverMock.Object);

            var logger = Mock.Of<ILogger<CacheService>>();

            return new CacheService(connMock.Object, logger);
        }
        
    }
}

