using Data.Interfaces;
using Data.Models;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _userRepository;
        private readonly ILogger<UserServices> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public UserServices(
            IUserRepository userRepository,
            ILogger<UserServices> logger,
            UserManager<ApplicationUser> userManager)
        {
            _userRepository = userRepository;
            _logger = logger;
            _userManager = userManager;
        }

        public async Task<Result<GetUserInfoResponse>> GetUserInfoAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogError("Error. Email is required");
                    return Result<GetUserInfoResponse>.Bad(
                        "Validation error",
                        StatusCodes.Status404NotFound,
                        new List<string> { "Email is required" }
                        );

                }

                var result = await _userRepository.GetUserInfoAsync(email);

                if (result == null)
                {
                    _logger.LogError("Error. Cannto find user");
                    return Result<GetUserInfoResponse>.Bad(
                        "Error",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "Cannto find user" }
                        );
                }

                return Result<GetUserInfoResponse>.Good(
                    "UserName changed successfully.",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while get info for user with email {Email}.", email);
                return Result<GetUserInfoResponse>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<IdentityResult>> ChangeUserNameAsync(string email, string userName) 
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Unauthorize: email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Unauthorize.",
                        StatusCodes.Status401Unauthorized,
                        new List<string> { "Email is null or empty" }
                        );
                }

                if (string.IsNullOrWhiteSpace(userName) || string.IsNullOrWhiteSpace(userName))
                {
                    _logger.LogWarning("Invalid input: userName is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Validatin error.", 
                        StatusCodes.Status400BadRequest,
                        new List<string> { "User name is null or empty" });
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User with this email {Email} dosn't exist", email);
                    return Result<IdentityResult>.Bad(
                        $"User with this email {email} dosn't exist", 
                        StatusCodes.Status404NotFound,
                        new List<string> { "UserDoNotExist" }
                        );
                }

                var isUserNameExist = await _userManager.FindByNameAsync(userName);

                if (isUserNameExist != null)
                { 
                    _logger.LogWarning("User with this userName {UserName} already exist", userName);

                    return Result<IdentityResult>.Bad(
                        $"User with this userName {userName} already exist", 
                        StatusCodes.Status409Conflict,
                        new List<string> { "UserNameAlreadyExist" }
                        );
                }

                var userNameChangeResult = await _userManager.SetUserNameAsync(user, userName);
                if (!userNameChangeResult.Succeeded)
                { 
                    _logger.LogError("Cannot set new userName");
                    return Result<IdentityResult>.Bad(
                        "Cannot set new userName", 
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotSetNewUserName" }
                        );
                }

                user.NormalizedUserName = _userManager.NormalizeName(userName);

                var result = await _userManager.UpdateAsync(user);

                if (!result.Succeeded)
                {
                    var error = string
                        .Join(", ", result.Errors.Select(e => e.Description));
                    
                    _logger.LogError("Failed to change UserName for user with email {Email}. Errors: {Errors}", email, error);
                    
                    error = error.ToList().Count > 0 ? error : "Failed to change UserName for unknown reasons.";
                    return Result<IdentityResult>.Bad(
                        error, 
                        StatusCodes.Status500InternalServerError,
                        new List<string> { error }
                        );
                }

                return Result<IdentityResult>.Good(
                    "UserName changed successfully.", 
                    StatusCodes.Status200OK,
                    userNameChangeResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing UserName for user with email {Email}.", email);
                return Result<IdentityResult>.Bad(
                    "An unexpected error occurred.", 
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<IdentityResult>> ChangeUserEmailAsync(string oldEmail, string newEmail)
        {
            try
            {
                if (string.IsNullOrEmpty(oldEmail) || string.IsNullOrWhiteSpace(oldEmail))
                {
                    _logger.LogWarning("Unauthorize: old email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Unauthorize.",
                        StatusCodes.Status401Unauthorized,
                        new List<string> { "Old email is null or empty" }
                        );
                }

                if (string.IsNullOrEmpty(newEmail) || string.IsNullOrWhiteSpace(newEmail))
                {
                    _logger.LogWarning("Unauthorize: New email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Unauthorize.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "New email is null or empty" }
                        );
                }

                var user = await _userManager.FindByEmailAsync(oldEmail);
                if (user == null)
                {
                    _logger.LogWarning("User with this email {oldEmail} dosn't exist", oldEmail);
                    return Result<IdentityResult>.Bad(
                        $"User with this email {oldEmail} dosn't exist",
                        StatusCodes.Status404NotFound,
                        new List<string> { "UserDoNotExist" }
                        );
                }

                var isNewEmailExist = await _userManager.FindByEmailAsync(newEmail);
                if (isNewEmailExist != null)
                {
                    _logger.LogWarning("User with this email {oldEmail} already exist", oldEmail);
                    return Result<IdentityResult>.Bad(
                        $"User with this email {oldEmail} already exist",
                        StatusCodes.Status409Conflict,
                        new List<string> { "UserAlreadyExist" }
                        );
                }

                var setNewEmail = await _userManager.SetEmailAsync(user, newEmail);
                if (!setNewEmail.Succeeded)
                {
                    _logger.LogError("Cannot set new email");
                    return Result<IdentityResult>.Bad(
                        "Cannot set new email",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotSetNewEmail" }
                        );
                }

                user.NormalizedEmail = _userManager.NormalizeEmail(newEmail);

                var result = await _userManager.UpdateAsync(user);
                if (!result.Succeeded)
                {
                    var error = string
                        .Join(", ", result.Errors.Select(e => e.Description));
                    
                    _logger.LogError("Failed to change email for user with email {oldEmail}. Errors: {Errors}", oldEmail, error);
                    
                    error = error.ToList().Count > 0 ? error : "Failed to change email for unknown reasons.";
                    return Result<IdentityResult>.Bad(
                        error,
                        StatusCodes.Status500InternalServerError,
                        new List<string> { error }
                        );
                }

                return Result<IdentityResult>.Good(
                    "Email changed successfully.",
                    StatusCodes.Status200OK,
                    setNewEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing email for user with email {Email}.", oldEmail);
                return Result<IdentityResult>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }
    }
}
