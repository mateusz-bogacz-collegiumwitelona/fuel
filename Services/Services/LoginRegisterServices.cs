using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;

namespace Services.Services
{
    public class LoginRegisterServices : ILoginRegisterServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly ILogger<LoginRegisterServices> _logger;
        private EmailSender _email;
        private readonly IProposalStatisticRepository _proposalStatisticRepository;
        private readonly IHttpContextAccessor _httpContext;
        private readonly IRefreshTokenRepository _refreshTokenRepository;
        private readonly ITokenFactory _tokenFactory;

        public LoginRegisterServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            ILogger<LoginRegisterServices> logger,
            EmailSender email,
            IProposalStatisticRepository proposalStatisticRepository,
            IHttpContextAccessor httpContext,
            IRefreshTokenRepository refreshTokenRepository,
            ITokenFactory tokenFactory
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _logger = logger;
            _email = email;
            _proposalStatisticRepository = proposalStatisticRepository;
            _httpContext = httpContext;
            _refreshTokenRepository = refreshTokenRepository;
            _tokenFactory = tokenFactory;
        }

        public async Task<Result<LoginResponse>> HandleLoginAsync(LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed. User with email {Email} not found.", request.Email);

                    return Result<LoginResponse>.Bad(
                        $"Can't find user with this email: {request.Email}",
                        StatusCodes.Status404NotFound
                        );
                }

                var result = await _signInManager.PasswordSignInAsync(
                    user,
                    request.Password,
                    false,
                    true
                    );

                if (!result.Succeeded)
                {
                    _logger.LogWarning("Invalid login attempt for user with email {Email}.", request.Email);
                    return Result<LoginResponse>.Bad(
                        "Invalid login attempt.",
                        StatusCodes.Status401Unauthorized
                        );
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles == null || !roles.Any())
                {
                    _logger.LogWarning("User with email {Email} has no roles assigned.", request.Email);
                    return Result<LoginResponse>.Bad(
                        "User has no roles assigned.",
                        StatusCodes.Status403Forbidden
                        );
                }

                var jwtToken = _tokenFactory.CreateJwtToken(user, roles);
                var jwtToString = new JwtSecurityTokenHandler().WriteToken(jwtToken);
                var context = _httpContext.HttpContext;
                var refreshToken = _tokenFactory.CreateRefreshToken(
                    user.Id,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers["User-Agent"].ToString()
                    );

                await _refreshTokenRepository.AddAsync(refreshToken);
                await _refreshTokenRepository.SaveChangesAsync();

                SetAuthCookie(context, jwtToString, jwtToken.ValidTo, refreshToken);

                context.Response.Headers.Append("X-Token-Expiry", jwtToken.ValidTo.ToString("o"));

                _logger.LogInformation("Login successful for user {Email}.", request.Email);

                var response = new LoginResponse
                {
                    Message = "Login successful.",
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList()
                };

                return Result<LoginResponse>.Good(
                    "Login successful.",
                    StatusCodes.Status200OK,
                    response
                );

            }
            catch (Exception ex)
            {
                return Result<LoginResponse>.Bad(
                    "An error occurred during login.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<Result<IdentityResult>> LogoutAsync()
        {
            try
            {
                await _signInManager.SignOutAsync();

                var context = _httpContext.HttpContext;

                var isDev = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

                var cookieOptions = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = !isDev,
                    SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                    Path = "/"
                };

                context.Response.Cookies.Delete("jwt", cookieOptions);
                context.Response.Cookies.Delete("refresh_token", cookieOptions);

                _logger.LogInformation("User logged out successfully.");

                return Result<IdentityResult>.Good("Logout successful.", StatusCodes.Status200OK);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during logout.");
                return Result<IdentityResult>.Bad(
                    "An error occurred during logout.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<Result<LoginResponse>> HandleRefreshAsync()
        {
            try
            {
                var context = _httpContext.HttpContext;
                var refreshTokenCookie = context.Request.Cookies["refresh_token"];

                if (string.IsNullOrEmpty(refreshTokenCookie))
                {
                    _logger.LogWarning("Refresh token cookie is missing.");
                    return Result<LoginResponse>.Bad(
                        "Refresh token is missing.",
                        StatusCodes.Status401Unauthorized
                    );
                }

                var storedToken = await _refreshTokenRepository.GetByTokenAsync(refreshTokenCookie);
                if (storedToken == null || storedToken.IsRevoked || storedToken.ExpiryDate <= DateTime.UtcNow)
                {
                    _logger.LogWarning("Invalid or expired refresh token.");
                    return Result<LoginResponse>.Bad(
                        "Invalid or expired refresh token.",
                        StatusCodes.Status401Unauthorized
                    );
                }

                var user = await _userManager.FindByIdAsync(storedToken.UserId.ToString());
                if (user == null)
                {
                    _logger.LogWarning("User not found for the provided refresh token.");
                    return Result<LoginResponse>.Bad(
                        "User not found.",
                        StatusCodes.Status404NotFound
                    );
                }

                storedToken.IsRevoked = true;
                storedToken.RevokedAt = DateTime.UtcNow;
                var roles = await _userManager.GetRolesAsync(user);

                if (roles == null || !roles.Any())
                {
                    _logger.LogWarning("User has no roles assigned.");
                    return Result<LoginResponse>.Bad(
                        "User has no roles assigned.",
                        StatusCodes.Status403Forbidden
                    );
                }

                var jwtToken = _tokenFactory.CreateJwtToken(user, roles);
                var jwtToString = new JwtSecurityTokenHandler().WriteToken(jwtToken);

                var newRefreshToken = _tokenFactory.CreateRefreshToken(
                    user.Id,
                    context.Connection.RemoteIpAddress?.ToString(),
                    context.Request.Headers["User-Agent"].ToString()
                    );

                await _refreshTokenRepository.UpdateAsync(storedToken);
                await _refreshTokenRepository.AddAsync(newRefreshToken);
                await _refreshTokenRepository.SaveChangesAsync();

                SetAuthCookie(context, jwtToString, jwtToken.ValidTo, newRefreshToken);

                context.Response.Headers.Append("X-Token-Expiry", jwtToken.ValidTo.ToString("o"));

                var response = new LoginResponse
                {
                    Message = "Token refreshed successfully.",
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList()
                };

                _logger.LogInformation("Refreshed token for user {Email}", user.Email);

                return Result<LoginResponse>.Good(
                    "Token refreshed.",
                    StatusCodes.Status200OK,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred during token refresh.");
                return Result<LoginResponse>.Bad(
                    "An error occurred during token refresh.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<Result<LoginResponse>> GetCurrentUserAsync(Guid userId)
        {
            try
            {
                if (userId == Guid.Empty)
                {
                    _logger.LogWarning("User ID is empty.");
                    return Result<LoginResponse>.Bad(
                        "User ID is required.",
                        StatusCodes.Status400BadRequest
                    );
                }

                var user = await _userManager.FindByIdAsync(userId.ToString());

                if (user == null)
                {
                    _logger.LogWarning("User not found for the provided email.");
                    return Result<LoginResponse>.Bad(
                        "User not found.",
                        StatusCodes.Status404NotFound
                    );
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles == null || !roles.Any())
                {
                    _logger.LogWarning("User has no roles assigned.");
                    return Result<LoginResponse>.Bad(
                        "User has no roles assigned.",
                        StatusCodes.Status403Forbidden
                    );
                }

                var response = new LoginResponse
                {
                    Message = "User retrieved successfully.",
                    Email = user.Email,
                    UserName = user.UserName,
                    Roles = roles.ToList()
                };

                _logger.LogInformation("Retrieved user {Email}", user.Email);

                return Result<LoginResponse>.Good(
                    "User retrieved successfully.",
                    StatusCodes.Status200OK,
                    response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user.");
                return Result<LoginResponse>.Bad(
                    "An error occurred while retrieving user.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        private void SetAuthCookie(HttpContext context, string token, DateTime expiry, RefreshToken refresh)
        {
            var isDev = context.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();

            var cookieOptions = new CookieOptions
            {
                HttpOnly = true,
                Secure = !isDev,
                SameSite = isDev ? SameSiteMode.Lax : SameSiteMode.None,
                Path = "/"
            };

            context.Response.Cookies.Append("jwt", token, new CookieOptions
            {
                HttpOnly = cookieOptions.HttpOnly,
                Secure = cookieOptions.Secure,
                SameSite = cookieOptions.SameSite,
                Expires = expiry,
                Path = cookieOptions.Path
            });

            context.Response.Cookies.Append("refresh_token", refresh.Token, new CookieOptions
            {
                HttpOnly = cookieOptions.HttpOnly,
                Secure = cookieOptions.Secure,
                SameSite = cookieOptions.SameSite,
                Expires = refresh.ExpiryDate,
                Path = cookieOptions.Path
            });
        }

        public async Task<Result<IdentityResult>> RegisterNewUserAsync(RegisterNewUserRequest request)
        {
            try
            {
                var isEmailExist = await _userManager.FindByEmailAsync(request.Email);

                if (isEmailExist != null)
                {
                    _logger.LogWarning("Attempt to register with existing email: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        $"User with this email: {request.Email} already exists",
                        StatusCodes.Status400BadRequest
                    );
                }

                var isUserNameExist = await _userManager.FindByNameAsync(request.UserName);

                if (isUserNameExist != null)
                {
                    _logger.LogWarning("Attempt to register with existing username: {UserName}", request.UserName);
                    return Result<IdentityResult>.Bad(
                        $"User with this username: {request.UserName} already exists",
                        StatusCodes.Status400BadRequest
                    );
                }

                var user = new ApplicationUser
                {
                    Id = Guid.NewGuid(),
                    UserName = request.UserName,
                    NormalizedUserName = request.UserName.ToUpper(),
                    Email = request.Email,
                    NormalizedEmail = request.Email.ToUpper(),
                    EmailConfirmed = false,
                    SecurityStamp = Guid.NewGuid().ToString(),
                    ConcurrencyStamp = Guid.NewGuid().ToString(),
                    Points = 0,
                    CreatedAt = DateTime.UtcNow
                };

                var createUser = await _userManager.CreateAsync(user, request.Password);
                if (!createUser.Succeeded)
                {
                    var errors = createUser.Errors.Select(e => e.Description).ToList();
                    var errorMessage = string.Join(", ", errors);
                    _logger.LogError("Error occurred while creating user {UserName}: {Errors}", request.UserName, errorMessage);

                    return Result<IdentityResult>.Bad(
                        "Error occurred while creating user",
                        StatusCodes.Status500InternalServerError,
                        errors
                    );
                }

                string defaultRole = "User";

                if (!await _roleManager.RoleExistsAsync(defaultRole))
                {
                    _logger.LogError("Default role {Role} does not exist.", defaultRole);
                    return Result<IdentityResult>.Bad(
                        $"Default role '{defaultRole}' does not exist.",
                        StatusCodes.Status500InternalServerError
                    );
                }

                var addToRole = await _userManager.AddToRoleAsync(user, defaultRole);

                if (!addToRole.Succeeded)
                {
                    var errors = addToRole.Errors.Select(e => e.Description).ToList();
                    var errorMessage = string.Join(", ", errors);
                    _logger.LogError("Failed to assign role '{Role}' to user {Email}. Errors: {Errors}",
                        defaultRole, request.Email, errorMessage);

                    return Result<IdentityResult>.Bad(
                        $"Failed to assign role '{defaultRole}' to user",
                        StatusCodes.Status500InternalServerError,
                        errors
                    );
                }

                var isProposalStatAdded = await _proposalStatisticRepository.AddProposalStatisticRecordAsync(request.Email);

                if (!isProposalStatAdded)
                {
                    _logger.LogError("Failed to create proposal statistics record for user {Email}.", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Failed to create proposal statistics record for user.",
                        StatusCodes.Status500InternalServerError
                    );
                }

                var confirmToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);

                if (string.IsNullOrEmpty(confirmToken))
                {
                    _logger.LogError("Failed to generate email confirmation token for user {Email}.", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Failed to generate email confirmation token.",
                        StatusCodes.Status500InternalServerError
                    );
                }

                var sendEmailConfirmation = await _email.SendRegisterConfirmEmailAsync(
                    request.Email,
                    request.UserName,
                    confirmToken
                );

                if (!sendEmailConfirmation)
                {
                    _logger.LogError("Failed to send confirmation email to: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "User registered but failed to send confirmation email",
                        StatusCodes.Status500InternalServerError
                    );
                }

                _logger.LogInformation("User registered successfully: {Email}", request.Email);
                return Result<IdentityResult>.Good(
                    "User registered successfully. Please check your email to confirm your account.",
                    StatusCodes.Status201Created
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during user registration for email: {Email}", request.Email);
                return Result<IdentityResult>.Bad(
                    "An error occurred during registration",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("Email confirmation attempt for non-existent email: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "User not found",
                        StatusCodes.Status404NotFound
                    );
                }

                if (user.EmailConfirmed)
                {
                    _logger.LogInformation("Email already confirmed for user: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Email is already confirmed",
                        StatusCodes.Status400BadRequest
                    );
                }

                var result = await _userManager.ConfirmEmailAsync(user, request.Token);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    var errorMessage = string.Join(", ", errors);
                    _logger.LogWarning("Email confirmation failed for {Email}. Errors: {Errors}",
                        request.Email, errorMessage);

                    return Result<IdentityResult>.Bad(
                        "Invalid or expired confirmation token",
                        StatusCodes.Status400BadRequest,
                        errors
                    );
                }

                _logger.LogInformation("Email successfully confirmed for user: {Email}", request.Email);

                return Result<IdentityResult>.Good(
                    "Email confirmed successfully. You can now log in.",
                    StatusCodes.Status200OK,
                    result
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during email confirmation for: {Email}", request.Email);
                return Result<IdentityResult>.Bad(
                    "An error occurred during email confirmation",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<IdentityResult>> ForgotPasswordAsync(string email)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed. User with email {Email} not found.", email);

                    return Result<IdentityResult>.Bad(
                        "User not found",
                        StatusCodes.Status404NotFound
                        );
                }

                if (!user.EmailConfirmed)
                {
                    _logger.LogInformation("Email cannot confirmed for user: {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "Email cannot confirmed",
                        StatusCodes.Status400BadRequest
                    );
                }

                string token = await _userManager.GeneratePasswordResetTokenAsync(user);

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Failed to generate password reset token for user {Email}.", email);
                    return Result<IdentityResult>.Bad(
                        "Failed to generate password reset token.",
                        StatusCodes.Status500InternalServerError
                    );
                }

                var sendEmailResetPassword = await _email.SendResetPasswordEmailAsync(
                    email,
                    user.UserName,
                    token
                );

                if (!sendEmailResetPassword)
                {
                    _logger.LogError("Failed to send reset password email to: {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "Failed to send reset password email",
                        StatusCodes.Status500InternalServerError
                    );
                }

                _logger.LogInformation("Password reset email sent successfully to: {Email}", email);
                return Result<IdentityResult>.Good(
                    "Password reset email sent successfully. Please check your email.",
                    StatusCodes.Status200OK
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during forgot password process for email: {Email}", email);
                return Result<IdentityResult>.Bad(
                    "An error occurred during the forgot password process",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<IdentityResult>> SetNewPassowrdAsync(ResetPasswordRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
                    _logger.LogWarning("Login attempt failed. User with email {Email} not found.", request.Email);

                    return Result<IdentityResult>.Bad(
                        "User not found",
                        StatusCodes.Status404NotFound
                        );
                }

                if (!user.EmailConfirmed)
                {
                    _logger.LogInformation("Email cannot confirmed for user: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Email cannot confirmed",
                        StatusCodes.Status400BadRequest
                    );
                }

                var decodedToken = Uri.UnescapeDataString(request.Token);
                var result = await _userManager.ResetPasswordAsync(
                    user,
                    decodedToken,
                    request.Password
                    );

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    var errorMessage = string.Join(", ", errors);

                    _logger.LogWarning("Password reset failed for {Email}. Errors: {Errors}",
                        request.Email, errorMessage);

                    return Result<IdentityResult>.Bad(
                        "Invalid or expired password reset token",
                        StatusCodes.Status400BadRequest,
                        errors
                    );
                }

                _logger.LogInformation("Password reset successfully for {Email}", request.Email);

                return Result<IdentityResult>.Good(
                    "Password reset successfully. You can now log in with your new password.",
                    StatusCodes.Status200OK,
                    result
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during password reset for: {Email}", request.Email);
                return Result<IdentityResult>.Bad(
                    "An error occurred during password reset",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
