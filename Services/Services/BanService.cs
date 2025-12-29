using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Serilog.Core;
using Services.Event;
using Services.Event.Interfaces;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class BanService : IBanService
    {
        private readonly IBanRepository _banRepository;
        private readonly ILogger<BanService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private IEmailSender _email;
        private readonly IReportRepositry _reportRepositry;
        private readonly CacheService _cache;
        private readonly IEventDispatcher _eventDispatcher;

        public BanService(
            IBanRepository banRepository,
            ILogger<BanService> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IEmailSender email,
            IReportRepositry reportRepositry,
            CacheService cache,
            IEventDispatcher eventDispatcher)
        {
            _banRepository = banRepository;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _email = email;
            _reportRepositry = reportRepositry;
            _cache = cache;
            _eventDispatcher = eventDispatcher;
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

                await _banRepository.DeactivateActiveBansAsync(user.Id, admin.Id);

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

                var addBanRecord = await _banRepository.AddBanRecordAsync(user, admin, request);

                if (!addBanRecord)
                {
                    _logger.LogError("Cannot add ban record for user {Email}", request.Email);
                    return Result<IdentityResult>.Bad(
                        $"Cannot add ban record for user with email {request.Email}.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotAddBanRecord" }
                    );
                }

                await _eventDispatcher.PublishAsync(new UserBannedEvent(user, admin, request.Reason, request.Days));

                var message = request.Days.HasValue
                    ? $"User banned successfully for {request.Days.Value} days"
                    : "User banned permanently";

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

        public async Task<Result<IdentityResult>> UnlockUserAsync(string adminEmail, string userEmail)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userEmail))
                {
                    _logger.LogWarning("Email is null or empty.");
                    return Result<IdentityResult>.Bad(
                        "Email is required.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "EmailIsNullOrEmpty" }
                    );
                }

                var user = await _userManager.FindByEmailAsync(userEmail);
                if (user == null)
                {
                    _logger.LogWarning("User with email '{Email}' does not exist.", userEmail);
                    return Result<IdentityResult>.Bad(
                        $"User with email {userEmail} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "UserNotFound" }
                    );
                }

                if (await _userManager.IsInRoleAsync(user, "Admin"))
                {
                    _logger.LogWarning("Cannot ban an admin: {Email}", adminEmail);
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

                var isLockedOut = await _userManager.IsLockedOutAsync(user);

                if (!isLockedOut)
                {
                    _logger.LogWarning("User {Email} is not locked out", userEmail);
                    return Result<IdentityResult>.Bad(
                        "User is not locked out.",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "UserNotLockedOut" }
                    );
                }

                await _banRepository.DeactivateActiveBansAsync(user.Id, admin.Id);

                var unlockResult = await _userManager.SetLockoutEndDateAsync(user, null);

                if (!unlockResult.Succeeded)
                {
                    _logger.LogError("Cannot unlock user {Email}", userEmail);
                    return Result<IdentityResult>.Bad(
                        "Failed to unlock user.",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "CannotUnlockUser" }
                    );
                }

                await _eventDispatcher.PublishAsync(new UserUnlockedEvent(user, admin));

                return Result<IdentityResult>.Good(
                    "User unlocked successfully",
                    StatusCodes.Status200OK,
                    unlockResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception occurred during unlocking user: {Email}", userEmail);
                return Result<IdentityResult>.Bad(
                    "An error occurred during user unlock",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<ReviewUserBanResponses>> GetUserBanInfoAsync(string email)
        {
            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(email))
                {
                    _logger.LogWarning("Unauthorize: email is null or empty.");
                    return Result<ReviewUserBanResponses>.Bad(
                        "Unauthorize.",
                        StatusCodes.Status401Unauthorized,
                        new List<string> { "Email is null or empty" }
                        );
                }
                var banInfo = await _banRepository.GetUserBanInfoAsync(email);
                if (banInfo == null)
                {
                    _logger.LogWarning("No ban information found for user with email {Email}.", email);
                    return Result<ReviewUserBanResponses>.Bad(
                        "No ban information found for the user.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "NoBanInfoFound" }
                        );
                }
                return Result<ReviewUserBanResponses>.Good(
                    "Ban information retrieved successfully.",
                    StatusCodes.Status200OK,
                    banInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving ban info for user with email {Email}.", email);
                return Result<ReviewUserBanResponses>.Bad(
                    "An unexpected error occurred.",
                    StatusCodes.Status500InternalServerError);
            }
        }
    }
}
