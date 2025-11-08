using Data.Models;
using DTO.Requests;
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

    }
}
