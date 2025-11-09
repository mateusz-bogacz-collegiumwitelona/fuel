using Data.Interfaces;
using Data.Models;
using DTO.Requests;
using DTO.Responses;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Services.Helpers;
using Services.Interfaces;

namespace Services.Services
{
    public class ReportService : IReportService
    {
        private readonly IReportRepositry _reportRepositry;
        private readonly ILogger<ReportService> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole<Guid>> _roleManager;
        private readonly IBanService _banService;

        public ReportService(
            IReportRepositry reportRepositry,
            ILogger<ReportService> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager,
            IBanService banService)
        {
            _reportRepositry = reportRepositry;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
            _banService = banService;
        }

        public async Task<Result<bool>> ReportUserAsync(string notifierEmail, ReportRequest request)
        {
            try
            {
                if (string.IsNullOrEmpty(request.ReportedEmail))
                {
                    _logger.LogWarning("Reported email is null or empty");
                    return Result<bool>.Bad(
                        "Reported email is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                        );
                }

                if (string.IsNullOrEmpty(notifierEmail))
                {
                    _logger.LogWarning("Reported email is null or empty");
                    return Result<bool>.Bad(
                        "Reported email is required",
                        StatusCodes.Status401Unauthorized,
                        new List<string> { "UnAauthorize" }
                        );
                }

                if (string.IsNullOrEmpty(request.Reason))
                {
                    _logger.LogWarning("Reason for reporting is null or empty");
                    return Result<bool>.Bad(
                        "Reason for reporting is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                        );
                }

                var notifier = await _userManager.FindByEmailAsync(notifierEmail);
                if (notifier == null)
                {
                    _logger.LogWarning("Notifier with email {Email} not found.", notifierEmail);
                    return Result<bool>.Bad(
                        "Notifier not found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "NotFound" }
                        );
                }

                var reportedUser = await _userManager.FindByEmailAsync(request.ReportedEmail);
                if (reportedUser == null)
                {
                    _logger.LogWarning("Reported user with email {Email} not found.", request.ReportedEmail);
                    return Result<bool>.Bad(
                        "Reported user not found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "NotFound" }
                        );
                }

                if (notifier.Id.Equals(reportedUser.Id))
                {
                    _logger.LogWarning("User with email {Email} attempted to report themselves.", notifierEmail);
                    return Result<bool>.Bad(
                        "You cannot report yourself",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                        );
                }

                if (await _userManager.IsInRoleAsync(reportedUser, "Admin"))
                {
                    _logger.LogWarning("User with email {Email} attempted to report an admin.", notifierEmail);
                    return Result<bool>.Bad(
                        "You cannot report an admin",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                        );
                }

                var result = await _reportRepositry.ReportUserAsync(
                    reportedUser,
                    notifier,
                    request.Reason
                    );

                if (!result)
                {
                    _logger.LogError("Failed to report user with email {Email}.", request.ReportedEmail);
                    return Result<bool>.Bad(
                        "Failed to report user",
                        StatusCodes.Status500InternalServerError,
                        new List<string> { "InternalError" }
                        );
                }

                return Result<bool>.Good(
                    "User reported successfully",
                    StatusCodes.Status200OK,
                    true
                    );
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while reporting user with email: {reportedEmail} by notifier: {notifierEmail}",
                    request.ReportedEmail,
                    notifierEmail);
                return Result<bool>.Bad(
                    "Internal Server Error",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }

        public async Task<Result<PagedResult<UserReportsResponse>>> GetUserReportAsync(string email, GetPaggedRequest pagged)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Reported email is null or empty");
                    return Result<PagedResult<UserReportsResponse>>.Bad(
                        "Reported email is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                    );
                }

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                {
                    _logger.LogWarning("Reported user with email {Email} not found.", email);
                    return Result<PagedResult<UserReportsResponse>>.Bad(
                        "Reported user not found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "NotFound" }
                    );
                }

                var result = await _reportRepositry.GetUserReportAsync(user.Id);

                if (result == null || !result.Any())
                {
                    _logger.LogWarning("No reports found for user with email {Email}.", email);

                    var emptyPage = new PagedResult<UserReportsResponse>
                    {
                        Items = new List<UserReportsResponse>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<UserReportsResponse>>.Good(
                        "No reports found.",
                        StatusCodes.Status200OK,
                        emptyPage
                    );
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<UserReportsResponse>>.Good(
                    "User reports retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while retrieving user reports for admin: {Message} | {InnerException}",
                    ex.Message, ex.InnerException);
                return Result<PagedResult<UserReportsResponse>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" }
                );
            }
        }
        public async Task<Result<IdentityResult>> ChangeReportStatusAsync(string adminEmail, ChangeReportStatusRequest request)
        {
            try
            {
                if (request.IsAccepted == null)
                {
                    _logger.LogWarning("IsAccepted field is null");
                    return Result<IdentityResult>.Bad(
                        "IsAccepted field is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                    );
                }

                if (string.IsNullOrEmpty(adminEmail))
                {
                    _logger.LogWarning("Admin email is null or empty");
                    return Result<IdentityResult>.Bad(
                        "Admin email is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
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

                var reportedUser = await _userManager.FindByEmailAsync(request.ReportedUserEmail);
                if (reportedUser == null)
                {
                    _logger.LogWarning("Reported user with email '{Email}' does not exist.", request.ReportedUserEmail);
                    return Result<IdentityResult>.Bad(
                        $"Reported user with email {request.ReportedUserEmail} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "ReportedUserNotFound" }
                    );
                }

                var reportingUser = await _userManager.FindByEmailAsync(request.ReportingUserEmail);
                if (reportingUser == null)
                {
                    _logger.LogWarning("Reporting user with email '{Email}' does not exist.", request.ReportingUserEmail);
                    return Result<IdentityResult>.Bad(
                        $"Reporting user with email {request.ReportingUserEmail} does not exist.",
                        StatusCodes.Status404NotFound,
                        new List<string> { "ReportingUserNotFound" }
                    );
                }

                if (request.IsAccepted)
                {
                    var changeStatus = await _reportRepositry.ChangeRepostStatusToAcceptedAsync(
                        reportedUser.Id,
                        reportingUser.Id,
                        admin,
                        request.ReportCreatedAt
                    );

                    if (!changeStatus)
                    {
                        _logger.LogError("Failed to change report status to accepted for reported user '{ReportedEmail}' by admin '{AdminEmail}'.",
                            request.ReportedUserEmail, adminEmail);
                        return Result<IdentityResult>.Bad(
                            "Failed to change report status to accepted.",
                            StatusCodes.Status500InternalServerError,
                            new List<string> { "InternalError" }
                        );
                    }

                    if (string.IsNullOrEmpty(request.Reason))
                    {
                        _logger.LogWarning("Ban reason is null or empty for reported user '{ReportedEmail}' by admin '{AdminEmail}'.",
                            request.ReportedUserEmail, adminEmail);
                        return Result<IdentityResult>.Bad(
                            "Ban reason is required when accepting a report.",
                            StatusCodes.Status400BadRequest,
                            new List<string> { "ValidationError" }
                        );
                    }

                    var ban = await _banService.LockoutUserAsync(
                        adminEmail,
                        new SetLockoutForUserRequest
                        {
                            Email = request.ReportedUserEmail,
                            Days = request.Days,
                            Reason = request.Reason
                        });

                    if (!ban.IsSuccess)
                    {
                        _logger.LogError("Failed to ban reported user '{ReportedEmail}' after accepting report by admin '{AdminEmail}'.",
                            request.ReportedUserEmail, adminEmail);
                        return Result<IdentityResult>.Bad(
                            "Failed to ban reported user after accepting report.",
                            StatusCodes.Status500InternalServerError,
                            new List<string> { "InternalError" }
                        );
                    }

                    _logger.LogInformation("Report accepted and user '{ReportedEmail}' banned by admin '{AdminEmail}'.",
                        request.ReportedUserEmail, adminEmail);

                    return Result<IdentityResult>.Good(
                        "Report accepted and user banned successfully.",
                        StatusCodes.Status200OK,
                        IdentityResult.Success
                    );
                }
                else
                {
                    var changeStatus = await _reportRepositry.ChangeRepostStatusToRejectAsync(
                        reportedUser.Id,
                        reportingUser.Id,
                        admin,
                        request.ReportCreatedAt
                    );

                    if (!changeStatus)
                    {
                        _logger.LogError("Failed to change report status to rejected for reported user '{ReportedEmail}' by admin '{AdminEmail}'.",
                            request.ReportedUserEmail, adminEmail);
                        return Result<IdentityResult>.Bad(
                            "Failed to change report status to rejected.",
                            StatusCodes.Status500InternalServerError,
                            new List<string> { "InternalError" }
                        );
                    }

                    _logger.LogInformation("Report rejected for user '{ReportedEmail}' by admin '{AdminEmail}'.",
                        request.ReportedUserEmail, adminEmail);

                    return Result<IdentityResult>.Good(
                        "Report rejected successfully.",
                        StatusCodes.Status200OK,
                        IdentityResult.Success
                    );
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "An error occurred while changing report status by admin: {AdminEmail}. Error: {Message}",
                    adminEmail, ex.Message);
                return Result<IdentityResult>.Bad(
                    "Internal Server Error",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { ex.Message }
                );
            }
        }
    }
}
