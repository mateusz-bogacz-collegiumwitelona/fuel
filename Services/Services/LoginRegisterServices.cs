using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Services.Helpers;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Services.Services
{
    public class LoginRegisterServices : ILoginRegisterServices
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;
        private readonly IEmailServices _emailServices;
        private readonly ILogger<LoginRegisterServices> _logger;

        public LoginRegisterServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IUserRepository userRepository,
            IEmailServices emailServices,
            ILogger<LoginRegisterServices> logger
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _userRepository = userRepository;
            _emailServices = emailServices;
            _logger = logger;
        }


        public async Task<Result<LoginResponse>> HandleLoginAsync(DTO.Requests.LoginRequest request)
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
                    true,
                    false
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

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email ?? ""),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
                };

                foreach (var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                var keyString = _configuration["Jwt:Key"];
                var issuer = _configuration["Jwt:Issuer"];
                var audience = _configuration["Jwt:Audience"];

                if (string.IsNullOrWhiteSpace(keyString) ||
                    string.IsNullOrWhiteSpace(issuer) ||
                    string.IsNullOrWhiteSpace(audience))
                {
                    _logger.LogError("JWT configuration is missing or invalid.");
                    return Result<LoginResponse>.Bad(
                        "Internal server error.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "JWT configuration is missing or invalid." }
                        );
                }

                if (keyString.Length < 32)
                {
                    _logger.LogError("JWT key length is insufficient. It must be at least 16 characters long.");
                    return Result<LoginResponse>.Bad(
                        "Internal server error.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "JWT key length is insufficient. It must be at least 16 characters long." }
                        );
                }

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                JwtSecurityToken token;

                try
                {
                    token = new JwtSecurityToken(
                                issuer: issuer,
                                audience: audience,
                                claims: claims,
                                expires: DateTime.UtcNow.AddHours(3),
                                signingCredentials: creds
                            );
                } catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, "Error creating JWT token for user {Email}.", request.Email);
                    return Result<LoginResponse>.Bad(
                        "Internal server error.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { $"{tokenEx.Message} | {tokenEx.InnerException}" }
                        );
                }

                var auth = new LoginResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo
                };
                _logger.LogInformation("JWT token successfully generated for user {Email}, expires at {Expiration}.", request.Email, token.ValidTo);

                return Result<LoginResponse>.Good(
                    "Login successful.",
                    StatusCodes.Status200OK,
                    auth
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

        public async Task<Result<ConfirmEmailRequest>> RegisterNewUser(RegisterNewUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "UserName is required." });

                if (string.IsNullOrWhiteSpace(request.Email))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Password))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Password is required." });

                if (string.IsNullOrWhiteSpace(request.ConfirmPassword))
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ConfirmPassword is required." });

                if (request.Password != request.ConfirmPassword)
                {
                    return Result<ConfirmEmailRequest>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Password and Confirm Password do not match." }
                        );
                }

                var result = await _userRepository.RegisterNewUser(request);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<ConfirmEmailRequest>.Bad(
                        "User registration failed",
                        StatusCodes.Status400BadRequest,
                        errors
                        );
                }

                string token = await _userRepository.GenerateConfirEmailTokenAsync(request.Email);

                if (string.IsNullOrWhiteSpace(token)) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to generate email confirmation token.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Token generation returned null or empty." }
                        );

                string encodedToken = Uri.EscapeDataString(token);

                if (string.IsNullOrWhiteSpace(encodedToken)) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to encode email confirmation token.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Encoded token is null or empty." }
                        );

                string confirmUrl = $"http://localhost:5000/api/auth/confirm-email?email={Uri.EscapeDataString(request.Email)}&token={encodedToken}";

                var sendEmail = await _emailServices.SendEmailConfirmationAsync(
                    request.Email,
                    request.UserName,
                    confirmUrl,
                    encodedToken
                    );

                if (!sendEmail.IsSuccess) return Result<ConfirmEmailRequest>.Bad(
                        "Failed to send email.",
                        StatusCodes.Status500InternalServerError
                        );

                var confirmEmailRequest = new ConfirmEmailRequest
                {
                    Email = request.Email,
                    Token = encodedToken
                };

                return Result<ConfirmEmailRequest>.Good(
                    "User registered successfully",
                    StatusCodes.Status201Created,
                    confirmEmailRequest
                    );
            }
            catch (Exception ex)
            {
                return Result<ConfirmEmailRequest>.Bad(
                    "An error occurred while registering the user.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }

        public async Task<Result<IdentityResult>> ConfirmEmailAsync(ConfirmEmailRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email)) return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Email is required." });

                if (string.IsNullOrWhiteSpace(request.Token)) return Result<IdentityResult>.Bad(
                        "Validation Error",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "Token is required." });

                var result = await _userRepository.ConfirmEmailAsync(request);

                if (!result.Succeeded)
                {
                    var errors = result.Errors.Select(e => e.Description).ToList();
                    return Result<IdentityResult>.Bad(
                        "Email confirmation failed",
                        StatusCodes.Status400BadRequest,
                        errors
                        );
                }

                return Result<IdentityResult>.Good(
                    "Email confirmed successfully",
                    StatusCodes.Status200OK,
                    result
                    );
            }
            catch (Exception ex)
            {
                return Result<IdentityResult>.Bad(
                    "An error occurred while confirming the email.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                    );
            }
        }
    }
}
