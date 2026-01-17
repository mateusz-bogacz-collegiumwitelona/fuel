using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Services.Event.Interfaces;
using Services.Helpers;
using Services.Interfaces;
using Services.Services;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace Tests.ServicesTests
{
    public class LoginRegisterServicesTest
    {
        private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
        private readonly Mock<SignInManager<ApplicationUser>> _signInManagerMock;
        private readonly Mock<RoleManager<IdentityRole<Guid>>> _roleManagerMock;
        private readonly Mock<ILogger<LoginRegisterServices>> _loggerMock;
        private readonly Mock<IEmailSender> _emailMock;
        private readonly Mock<IProposalStatisticRepository> _proposalStatisticRepositoryMock;
        private readonly Mock<IUserRepository> _userRepositoryMock;
        private readonly Mock<IHttpContextAccessor> _httpContextAccessorMock;
        private readonly Mock<IRefreshTokenRepository> _refreshTokenRepositoryMock;
        private readonly Mock<ITokenFactory> _tokenFactoryMock;
        private readonly Mock<IEventDispatcher> _eventDispatcherMock;
        private readonly LoginRegisterServices _service;

        public LoginRegisterServicesTest()
        {
            _userManagerMock = CreateUserManagerMock();
            _signInManagerMock = CreateSignInManagerMock();
            _roleManagerMock = CreateRoleManagerMock();
            _loggerMock = new Mock<ILogger<LoginRegisterServices>>();
            _emailMock = CreateEmailSenderMock();
            _proposalStatisticRepositoryMock = new Mock<IProposalStatisticRepository>();
            _userRepositoryMock = new Mock<IUserRepository>();
            _httpContextAccessorMock = new Mock<IHttpContextAccessor>();
            _refreshTokenRepositoryMock = new Mock<IRefreshTokenRepository>();
            _tokenFactoryMock = new Mock<ITokenFactory>();
            _eventDispatcherMock = new Mock<IEventDispatcher>();

            _service = new LoginRegisterServices(
                _userManagerMock.Object,
                _signInManagerMock.Object,
                _roleManagerMock.Object,
                new Mock<Microsoft.Extensions.Configuration.IConfiguration>().Object,
                _loggerMock.Object,
                _emailMock.Object,
                _proposalStatisticRepositoryMock.Object,
                _userRepositoryMock.Object,
                _httpContextAccessorMock.Object,
                _refreshTokenRepositoryMock.Object,
                _tokenFactoryMock.Object,
                _eventDispatcherMock.Object
            );
        }

       

        [Fact]
        public async Task HandleLoginAsync_UserNotFound_ReturnsBadResultWithStatus404()
        {
            // Arrange
            var request = new LoginRequest { Email = "nonexistent@example.com", Password = "password" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains("Can't find user", result.Message ?? string.Empty);
        }

        [Fact]
        public async Task HandleLoginAsync_UserIsDeleted_ReturnsBadResultWithStatus403()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com" };
            var request = new LoginRequest { Email = user.Email, Password = "password" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(ur => ur.IsUserDeleted(user))
                .ReturnsAsync(true);

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            Assert.Contains("deleted", (result.Message ?? string.Empty).ToLower());
        }

        [Fact]
        public async Task HandleLoginAsync_InvalidPassword_ReturnsBadResultWithStatus401()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = true };
            var request = new LoginRequest { Email = user.Email, Password = "wrongpassword" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(ur => ur.IsUserDeleted(user))
                .ReturnsAsync(false);
            _signInManagerMock.Setup(sm => sm.PasswordSignInAsync(user, request.Password, false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Failed);

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status401Unauthorized, result.StatusCode);
            Assert.Contains("Invalid login", result.Message ?? string.Empty);
        }

        [Fact]
        public async Task HandleLoginAsync_UserHasNoRoles_ReturnsBadResultWithStatus403()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = true };
            var request = new LoginRequest { Email = user.Email, Password = "password" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(ur => ur.IsUserDeleted(user))
                .ReturnsAsync(false);
            _signInManagerMock.Setup(sm => sm.PasswordSignInAsync(user, request.Password, false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(new List<string>());

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
            Assert.Contains("no roles", (result.Message ?? string.Empty).ToLower());
        }

        [Fact]
        public async Task HandleLoginAsync_SuccessfulLogin_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
            var roles = new List<string> { "User" };
            var request = new LoginRequest { Email = user.Email, Password = "password" };
            var jwtToken = new JwtSecurityToken(expires: DateTime.UtcNow.AddHours(1));
            var refreshToken = new RefreshToken { Token = "refresh123", ExpiryDate = DateTime.UtcNow.AddDays(7) };

            SetupSuccessfulLogin(user, roles, jwtToken, refreshToken);

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Email, result.Data.Email);
            Assert.Equal(roles, result.Data.Roles);
        }

        [Fact]
        public async Task HandleLoginAsync_ExceptionThrown_ReturnsBadResultWithStatus500()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@example.com", Password = "password" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(It.IsAny<string>()))
                .ThrowsAsync(new Exception("Database error"));

            // Act
            var result = await _service.HandleLoginAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.Contains("error occurred", (result.Message ?? string.Empty).ToLower());
        }

     

        [Fact]
        public async Task LogoutAsync_SuccessfulLogout_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Microsoft.Extensions.Hosting.Environments.Development);
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(envMock.Object);
            httpContext.RequestServices = services.BuildServiceProvider();

            _httpContextAccessorMock.Setup(hca => hca.HttpContext).Returns(httpContext);
            _signInManagerMock.Setup(sm => sm.SignOutAsync()).Returns(Task.CompletedTask);

            // Act
            var result = await _service.LogoutAsync();

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Contains("Logout successful", result.Message ?? string.Empty);
        }

        [Fact]
        public async Task LogoutAsync_ExceptionThrown_ReturnsBadResultWithStatus500()
        {
            // Arrange
            var httpContext = new DefaultHttpContext();

            var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Microsoft.Extensions.Hosting.Environments.Development);
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(envMock.Object);
            httpContext.RequestServices = services.BuildServiceProvider();

            _httpContextAccessorMock.Setup(hca => hca.HttpContext).Returns(httpContext);
            _signInManagerMock.Setup(sm => sm.SignOutAsync())
                .ThrowsAsync(new Exception("Signout error"));

            // Act
            var result = await _service.LogoutAsync();

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.Contains("error occurred", (result.Message ?? string.Empty).ToLower());
        }

     

        [Fact]
        public async Task RegisterNewUserAsync_EmailAlreadyExists_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = Guid.NewGuid(), Email = "existing@example.com" };
            var request = new RegisterNewUserRequest { Email = "existing@example.com", UserName = "newuser", Password = "Password123!" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _service.RegisterNewUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("already exists", result.Message ?? string.Empty);
        }

        [Fact]
        public async Task RegisterNewUserAsync_UserNameAlreadyExists_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var existingUser = new ApplicationUser { Id = Guid.NewGuid(), UserName = "existinguser" };
            var request = new RegisterNewUserRequest { Email = "new@example.com", UserName = "existinguser", Password = "Password123!" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.FindByNameAsync(request.UserName))
                .ReturnsAsync(existingUser);

            // Act
            var result = await _service.RegisterNewUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("username", (result.Message ?? string.Empty).ToLower());
        }

        [Fact]
        public async Task RegisterNewUserAsync_SuccessfulRegistration_ReturnsGoodResultWithStatus201()
        {
            // Arrange
            var request = new RegisterNewUserRequest { Email = "new@example.com", UserName = "newuser", Password = "Password123!" };
            var identityResult = IdentityResult.Success;

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.FindByNameAsync(request.UserName))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(identityResult);
            _roleManagerMock.Setup(rm => rm.RoleExistsAsync("User"))
                .ReturnsAsync(true);
            _userManagerMock.Setup(um => um.AddToRoleAsync(It.IsAny<ApplicationUser>(), "User"))
                .ReturnsAsync(identityResult);
            _proposalStatisticRepositoryMock.Setup(psr => psr.AddProposalStatisticRecordAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync(true);
            _userManagerMock.Setup(um => um.GenerateEmailConfirmationTokenAsync(It.IsAny<ApplicationUser>()))
                .ReturnsAsync("token123");
            _emailMock.Setup(e => e.SendRegisterConfirmEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.RegisterNewUserAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status201Created, result.StatusCode);
            Assert.Contains("registered successfully", (result.Message ?? string.Empty).ToLower());
        }

        [Fact]
        public async Task RegisterNewUserAsync_CreateUserFails_ReturnsBadResultWithStatus500()
        {
            // Arrange
            var request = new RegisterNewUserRequest { Email = "new@example.com", UserName = "newuser", Password = "Password123!" };
            var identityErrors = new[] { new IdentityError { Description = "Password too weak" } };
            var identityResult = IdentityResult.Failed(identityErrors);

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser?)null);
            _userManagerMock.Setup(um => um.FindByNameAsync(request.UserName))
                .ReturnsAsync((ApplicationUser)null);
            _userManagerMock.Setup(um => um.CreateAsync(It.IsAny<ApplicationUser>(), request.Password))
                .ReturnsAsync(identityResult);

            // Act
            var result = await _service.RegisterNewUserAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status500InternalServerError, result.StatusCode);
            Assert.Contains("error occurred", result.Message.ToLower());
        }

      

        [Fact]
        public async Task ConfirmEmailAsync_UserNotFound_ReturnsBadResultWithStatus404()
        {
            // Arrange
            var request = new ConfirmEmailRequest { Email = "nonexistent@example.com", Token = "token123" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _service.ConfirmEmailAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains("not found", result.Message.ToLower());
        }

        [Fact]
        public async Task ConfirmEmailAsync_EmailAlreadyConfirmed_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = true };
            var request = new ConfirmEmailRequest { Email = user.Email, Token = "token123" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _service.ConfirmEmailAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("already confirmed", result.Message.ToLower());
        }

        [Fact]
        public async Task ConfirmEmailAsync_SuccessfulConfirmation_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false };
            var request = new ConfirmEmailRequest { Email = user.Email, Token = "token123" };
            var identityResult = IdentityResult.Success;

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ConfirmEmailAsync(user, request.Token))
                .ReturnsAsync(identityResult);

            // Act
            var result = await _service.ConfirmEmailAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Contains("confirmed", result.Message.ToLower());
        }

        

    

        [Fact]
        public async Task ForgotPasswordAsync_UserNotFound_ReturnsBadResultWithStatus404()
        {
            // Arrange
            var email = "nonexistent@example.com";
            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains("not found", result.Message.ToLower());
        }

        [Fact]
        public async Task ForgotPasswordAsync_EmailNotConfirmed_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false };
            var email = user.Email;

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("cannot confirmed", result.Message.ToLower());
        }

        [Fact]
        public async Task ForgotPasswordAsync_SuccessfulReset_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = true, UserName = "testuser" };
            var email = user.Email;

            _userManagerMock.Setup(um => um.FindByEmailAsync(email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GeneratePasswordResetTokenAsync(user))
                .ReturnsAsync("resettoken123");
            _emailMock.Setup(e => e.SendResetPasswordEmailAsync(email, user.UserName, It.IsAny<string>()))
                .ReturnsAsync(true);

            // Act
            var result = await _service.ForgotPasswordAsync(email);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Contains("sent successfully", result.Message.ToLower());
        }

   

     

        [Fact]
        public async Task SetNewPassowrdAsync_UserNotFound_ReturnsBadResultWithStatus404()
        {
            // Arrange
            var request = new ResetPasswordRequest { Email = "nonexistent@example.com", Token = "token123", Password = "NewPassword123!" };
            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _service.SetNewPassowrdAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains("not found", result.Message.ToLower());
        }

        [Fact]
        public async Task SetNewPassowrdAsync_EmailNotConfirmed_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = false };
            var request = new ResetPasswordRequest { Email = user.Email, Token = "token123", Password = "NewPassword123!" };

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);

            // Act
            var result = await _service.SetNewPassowrdAsync(request);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("cannot confirmed", result.Message.ToLower());
        }

        [Fact]
        public async Task SetNewPassowrdAsync_SuccessfulReset_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", EmailConfirmed = true };
            var request = new ResetPasswordRequest { Email = user.Email, Token = "resettoken123", Password = "NewPassword123!" };
            var identityResult = IdentityResult.Success;

            _userManagerMock.Setup(um => um.FindByEmailAsync(request.Email))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.ResetPasswordAsync(user, It.IsAny<string>(), request.Password))
                .ReturnsAsync(identityResult);

            // Act
            var result = await _service.SetNewPassowrdAsync(request);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.Contains("reset successfully", result.Message.ToLower());
        }

     


        [Fact]
        public async Task GetCurrentUserAsync_EmptyUserId_ReturnsBadResultWithStatus400()
        {
            // Arrange
            var userId = Guid.Empty;

            // Act
            var result = await _service.GetCurrentUserAsync(userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
            Assert.Contains("required", result.Message.ToLower());
        }

        [Fact]
        public async Task GetCurrentUserAsync_UserNotFound_ReturnsBadResultWithStatus404()
        {
            // Arrange
            var userId = Guid.NewGuid();
            _userManagerMock.Setup(um => um.FindByIdAsync(userId.ToString()))
                .ReturnsAsync((ApplicationUser)null);

            // Act
            var result = await _service.GetCurrentUserAsync(userId);

            // Assert
            Assert.False(result.IsSuccess);
            Assert.Equal(StatusCodes.Status404NotFound, result.StatusCode);
            Assert.Contains("not found", result.Message.ToLower());
        }

        [Fact]
        public async Task GetCurrentUserAsync_SuccessfulRetrieval_ReturnsGoodResultWithStatus200()
        {
            // Arrange
            var user = new ApplicationUser { Id = Guid.NewGuid(), Email = "test@example.com", UserName = "testuser" };
            var roles = new List<string> { "User" };

            _userManagerMock.Setup(um => um.FindByIdAsync(user.Id.ToString()))
                .ReturnsAsync(user);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);

            // Act
            var result = await _service.GetCurrentUserAsync(user.Id);

            // Assert
            Assert.True(result.IsSuccess);
            Assert.Equal(StatusCodes.Status200OK, result.StatusCode);
            Assert.NotNull(result.Data);
            Assert.Equal(user.Email, result.Data.Email);
        }

    

        private Mock<UserManager<ApplicationUser>> CreateUserManagerMock()
        {
            var userStore = new Mock<IUserStore<ApplicationUser>>();
            return new Mock<UserManager<ApplicationUser>>(userStore.Object, null, null, null, null, null, null, null, null);
        }

        private Mock<SignInManager<ApplicationUser>> CreateSignInManagerMock()
        {
            var userManager = CreateUserManagerMock();
            var contextAccessor = new Mock<IHttpContextAccessor>();
            var claimsFactory = new Mock<IUserClaimsPrincipalFactory<ApplicationUser>>();

            return new Mock<SignInManager<ApplicationUser>>(
                userManager.Object,
                contextAccessor.Object,
                claimsFactory.Object,
                null,
                null,
                null,
                null
            );
        }

        private Mock<RoleManager<IdentityRole<Guid>>> CreateRoleManagerMock()
        {
            var roleStore = new Mock<IRoleStore<IdentityRole<Guid>>>();
            return new Mock<RoleManager<IdentityRole<Guid>>>(
                roleStore.Object,
                null,
                null,
                null,
                null
            );
        }
            
        private Mock<IEmailSender> CreateEmailSenderMock()
        {
           
            return new Mock<IEmailSender>();
        }

        private void SetupSuccessfulLogin(ApplicationUser user, List<string> roles, JwtSecurityToken jwtToken, RefreshToken refreshToken)
        {
            var httpContext = new DefaultHttpContext();
            var responseFeature = new HttpResponseFeature();
            httpContext.Features.Set<IHttpResponseFeature>(responseFeature);

            
            user.EmailConfirmed = true;

           
            var envMock = new Mock<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>();
            envMock.SetupGet(e => e.EnvironmentName).Returns(Microsoft.Extensions.Hosting.Environments.Development);
            var services = new ServiceCollection();
            services.AddSingleton<Microsoft.AspNetCore.Hosting.IWebHostEnvironment>(envMock.Object);
            httpContext.RequestServices = services.BuildServiceProvider();

            _userManagerMock.Setup(um => um.FindByEmailAsync(user.Email))
                .ReturnsAsync(user);
            _userRepositoryMock.Setup(ur => ur.IsUserDeleted(user))
                .ReturnsAsync(false);
            _signInManagerMock.Setup(sm => sm.PasswordSignInAsync(user, It.IsAny<string>(), false, true))
                .ReturnsAsync(Microsoft.AspNetCore.Identity.SignInResult.Success);
            _userManagerMock.Setup(um => um.GetRolesAsync(user))
                .ReturnsAsync(roles);
            _tokenFactoryMock.Setup(tf => tf.CreateJwtToken(user, roles))
                .Returns(jwtToken);
            _tokenFactoryMock.Setup(tf => tf.CreateRefreshToken(user.Id, It.IsAny<string>(), It.IsAny<string>()))
                .Returns(refreshToken);
            _httpContextAccessorMock.Setup(hca => hca.HttpContext)
                .Returns(httpContext);
            _refreshTokenRepositoryMock.Setup(rtr => rtr.AddAsync(It.IsAny<RefreshToken>()))
                .Returns(Task.CompletedTask);
            _refreshTokenRepositoryMock.Setup(rtr => rtr.SaveChangesAsync())
                .Returns(Task.CompletedTask);
        }

      
    }
}


