using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore.Metadata;
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
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private EmailSender _email;

        public UserServices(
            IUserRepository userRepository,
            ILogger<UserServices> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            EmailSender email)
        {
            _userRepository = userRepository;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _email = email;
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

        public async Task<Result<IdentityResult>> ChangeUserPasswordAsync(string email, ChangePasswordRequest request)
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

                if (request.NewPassword != request.ConfirmNewPassword)
                {
                    _logger.LogWarning("New password and confirm new password do not match for user with email {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "New password and confirm new password do not match",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "PasswordMismatch" }
                        );
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

                if (!await _userManager.CheckPasswordAsync(user, request.CurrentPassword))
                {
                    _logger.LogWarning("Current password is incorrect for user with email {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "Current password is incorrect",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "IncorrectCurrentPassword" }
                        );
                }

                var result = await _userManager.ChangePasswordAsync(user, request.CurrentPassword, request.NewPassword);

                if (!result.Succeeded)
                {
                    var error = string
                        .Join(", ", result.Errors.Select(e => e.Description));

                    _logger.LogError("Failed to change password for user with email {Email}. Errors: {Errors}", email, error);

                    error = error.ToList().Count > 0 ? error : "Failed to change password for unknown reasons.";
                    return Result<IdentityResult>.Bad(
                        error,
                        StatusCodes.Status500InternalServerError,
                        new List<string> { error }
                        );
                }

                return Result<IdentityResult>.Good(
                    "Password changed successfully.",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing password for user with email {Email}.", email);
                return Result<IdentityResult>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<IdentityResult>> DeleteUserAsyc(string email, DeleteAccountRequest request)
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

                if (request.Password != request.ConfirmPassword)
                {
                    _logger.LogWarning("Password and confirm password do not match for user with email {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "Password and confirm password do not match",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "PasswordMismatch" }
                        );
                }

                if (!await _userManager.CheckPasswordAsync(user, request.Password))
                {
                    _logger.LogWarning("Current password is incorrect for user with email {Email}", email);
                    return Result<IdentityResult>.Bad(
                        "Current password is incorrect",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "IncorrectCurrentPassword" }
                        );
                }

                var result = await _userRepository.DeleteUserAsync(user);

                if (!result.Succeeded)
                {
                    var error = string
                        .Join(", ", result.Errors.Select(e => e.Description));

                    _logger.LogError("Failed to delete account for user {Email}. Errors: {Errors}", email, error);

                    error = error.ToList().Count > 0 ? error : "Failed to change password for unknown reasons.";
                    return Result<IdentityResult>.Bad(
                        error,
                        StatusCodes.Status500InternalServerError,
                        new List<string> { error }
                        );
                }

                return Result<IdentityResult>.Good(
                    "User deleted successfully.",
                    StatusCodes.Status200OK,
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while deleting user with email {Email}.", email);
                return Result<IdentityResult>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<PagedResult<GetUserListResponse>>> GetUserListAsync(GetPaggedRequest pagged, TableRequest request)
        {
            try
            {
                var result = await _userRepository.GetUserListAsync(request);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No user found in the database.");

                    var emptyPage = new PagedResult<GetUserListResponse>
                    {
                        Items = new List<GetUserListResponse>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<GetUserListResponse>>.Good(
                        "No users found.",
                        StatusCodes.Status200OK,
                        emptyPage);
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<GetUserListResponse>>.Good(
                    "Users retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user list.");
                return Result<PagedResult<GetUserListResponse>>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }

        public async Task<Result<IdentityResult>> ChangeUserRoleAsync(string email, string newRole)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Email is required.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "EmailIsNullOrEmpty" }
                    );
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("User with email '{Email}' does not exist.", email);
                    return Result<IdentityResult>.Bad(
                        $"User with email {email} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "UserNotFound" }
                    );
                }

                if (!await _roleManager.RoleExistsAsync(newRole))
                {
                    _logger.LogWarning("Role '{Role}' does not exist.", newRole);
                    return Result<IdentityResult>.Bad(
                        $"Role {newRole} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "RoleNotFound" }
                    );
                }

                var currentRoles = await _userManager.GetRolesAsync(user);

                var removeResult = await _userManager.RemoveFromRolesAsync(user, currentRoles);
                if (!removeResult.Succeeded)
                {
                    _logger.LogError("Failed to remove current roles from user '{Email}'.", email);
                    return Result<IdentityResult>.Bad(
                        $"Failed to remove existing roles from {email}.",
                        StatusCodes.Status400BadRequest,
                        removeResult.Errors.Select(e => e.Description).ToList()
                    );
                }

                var addResult = await _userManager.AddToRoleAsync(user, newRole);
                if (!addResult.Succeeded)
                {
                    _logger.LogError("Failed to assign role '{Role}' to user '{Email}'.", newRole, email);
                    return Result<IdentityResult>.Bad(
                        $"Failed to assign role {newRole} to {email}.",
                        StatusCodes.Status400BadRequest,
                        addResult.Errors.Select(e => e.Description).ToList()
                    );
                }

                _logger.LogInformation("Successfully changed role for '{Email}' to '{Role}'.", email, newRole);

                return Result<IdentityResult>.Good(
                    $"Role changed successfully to {newRole}.",
                    StatusCodes.Status200OK,
                    addResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while changing role for '{Email}' to '{Role}'.", email, newRole);
                return Result<IdentityResult>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError
                );
            }
        }

        public async Task<Result<IdentityResult>> LockoutUserAsync(string adminEmail, SetLockoutForUserRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Email))
                {
                    _logger.LogWarning("Email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Email is required.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "EmailIsNullOrEmpty" }
                    );
                }

                if (string.IsNullOrWhiteSpace(request.Reason))
                {
                    _logger.LogWarning("Reason is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Reason is required.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ReasonIsNullOrEmpty" }
                    );
                }

                var user = await _userManager.FindByEmailAsync(request.Email);
                if (user == null)
                {
                    _logger.LogWarning("User with email '{Email}' does not exist.", request.Email);
                    return Result<IdentityResult>.Bad(
                        $"User with email {request.Email} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "UserNotFound" }
                    );
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogWarning("Cannot ban an admin: {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Cannot ban an admin.",
                        StatusCodes.Status403Forbidden,
                        new List<string> { "CannotBanAdmin" }
                    );
                }

                var admin = await _userManager.FindByEmailAsync(adminEmail);
                if (admin == null)
                {
                    _logger.LogWarning("Admin with email '{Email}' does not exist.", adminEmail);
                    return Result<IdentityResult>.Bad(
                        $"Admin with email {adminEmail} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "AdminNotFound" }
                    );
                }

                if (!await _userManager.IsInRoleAsync(admin, "Admin"))
                {
                    _logger.LogWarning("User '{AdminEmail}' is not an admin", adminEmail);
                    return Result<IdentityResult>.Bad(
                        $"User {adminEmail} is not an admin.",
                        StatusCodes.Status403Forbidden,
                        new List<string> { "NotAdmin" }
                    );
                }

                await _userRepository.DeactivateActiveBansAsync(user.Id, admin.Id);

                await _userManager.SetLockoutEnabledAsync(user, true);

                var lockoutEnd = request.Days.HasValue
                    ? DateTimeOffset.UtcNow.AddDays(request.Days.Value)
                    : DateTimeOffset.MaxValue;

                var banResult = await _userManager.SetLockoutEndDateAsync(user, lockoutEnd);

                if (!banResult.Succeeded)
                {
                    _logger.LogError("Cannot ban user {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        "Failed to ban user.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotBanUser" }
                    );
                }

                var addBanRecord = await _userRepository.AddBanRecordAsync(user, admin, request);

                if (!addBanRecord)
                {
                    _logger.LogError("Cannot add ban record for user {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        $"Cannot add ban record for user with email {request.Email}.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotAddBanRecord" }
                    );
                }

                var message = request.Days.HasValue
                    ? $"User banned successfully for {request.Days.Value} days"
                    : "User banned permanently";

                var sendEmail = await _email.SendLockoutEmailAsync(
                    user.Email,
                    user.UserName,
                    admin.UserName,
                    request.Days,
                    request.Reason
                    );

                if (!sendEmail) _logger.LogWarning("Failed to send lockout email to {Email}, but user was banned successfully", user.Email);

                _logger.LogInformation("User {Email} banned successfully. {BanType}",
                    request.Email,
                    request.Days.HasValue ? $"Duration: {request.Days.Value} days" : "Permanent");

                return Result<IdentityResult>.Good(
                    message,
                    StatusCodes.Status200OK,
                    banResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during banning user: {Email}", request.Email);
                return Result<IdentityResult>.Bad(
                    "An error occurred during user ban",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
