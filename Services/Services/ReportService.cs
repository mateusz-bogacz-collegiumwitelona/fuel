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

        public ReportService(
            IReportRepositry reportRepositry,
            ILogger<ReportService> logger,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole<Guid>> roleManager)
        {
            _reportRepositry = reportRepositry;
            _logger = logger;
            _userManager = userManager;
            _roleManager = roleManager;
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
                    notifier,
                    reportedUser,
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

        public async Task<Result<PagedResult<UserReportsRespnse>>> GetUserReportAsync(string email, GetPaggedRequest pagged)
        {
            try
            {
                if (string.IsNullOrEmpty(email))
                {
                    _logger.LogWarning("Reported email is null or empty");
                    return Result<PagedResult<UserReportsRespnse>>.Bad(
                        "Reported email is required",
                        StatusCodes.Status400BadRequest,
                        new List<string> { "ValidationError" }
                        );
                }

                var user = await _userManager.FindByEmailAsync(email);

                if (user == null)
                {
                    _logger.LogWarning("Reported user with email {Email} not found.", email);
                    return Result<PagedResult<UserReportsRespnse>>.Bad(
                        "Reported user not found",
                        StatusCodes.Status404NotFound,
                        new List<string> { "NotFound" }
                        );
                }

                var result = await _reportRepositry.GetUserReportAsync(user.Id);

                if (result == null)
                {
                    _logger.LogWarning("No stations found in the database.");

                    var emptyPage = new PagedResult<UserReportsRespnse>
                    {
                        Items = new List<UserReportsRespnse>(),
                        PageNumber = pagged.PageNumber ?? 1,
                        PageSize = pagged.PageSize ?? 10,
                        TotalCount = 0,
                        TotalPages = 0
                    };

                    return Result<PagedResult<UserReportsRespnse>>.Good(
                        "No stations found.",
                        StatusCodes.Status200OK,
                        emptyPage);
                }

                int pageNumber = pagged.PageNumber ?? 1;
                int pageSize = pagged.PageSize ?? 10;

                var pagedResult = result.ToPagedResult(pageNumber, pageSize);

                if (pagedResult.PageNumber > pagedResult.TotalPages && pagedResult.TotalPages > 0)
                    pagedResult = result.ToPagedResult(pagedResult.TotalPages, pageSize);

                return Result<PagedResult<UserReportsRespnse>>.Good(
                    "Station retrieved successfully",
                    StatusCodes.Status200OK,
                    pagedResult
                    );

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"An error occurred while retrieving user reposrts for admin: {ex.Message} | {ex.InnerException}");
                return Result<PagedResult<UserReportsRespnse>>.Bad(
                    "An error occurred while processing your request.",
                    StatusCodes.Status500InternalServerError,
                    new List<string> { $"{ex.Message} | {ex.InnerException}" });
            }
        }
    }
}
