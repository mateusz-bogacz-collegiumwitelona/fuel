using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Services.Helpers;
using Services.Interfaces;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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

        public LoginRegisterServices(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IConfiguration configuration,
            IUserRepository userRepository,
            IEmailServices emailServices
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _userRepository = userRepository;
            _emailServices = emailServices;
        }


        public async Task<Result<LoginResponse>> HandleLoginAsync(DTO.Requests.LoginRequest request)
        {
            try
            {
                var user = await _userManager.FindByEmailAsync(request.Email);

                if (user == null)
                {
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
                    return Result<LoginResponse>.Bad(
                        "Invalid login attempt.",
                        StatusCodes.Status401Unauthorized
                        );
                }

                var roles = await _userManager.GetRolesAsync(user);

                if (roles == null || !roles.Any())
                {
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

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
                var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken(
                    issuer: _configuration["Jwt:Issuer"],
                    audience: _configuration["Jwt:Audience"],
                    claims: claims,
                    expires: DateTime.Now.AddHours(3),
                    signingCredentials: creds
                    );

                var auth = new LoginResponse
                {
                    Token = new JwtSecurityTokenHandler().WriteToken(token),
                    Expiration = token.ValidTo
                };

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
