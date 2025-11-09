using Data.Context;
using Data.Enums;
using Data.Models;
using DTO.Responses;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Data.Reopsitories
{
    public class ReportRepositry : IReportRepositry
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ReportRepositry> _logger;

        public ReportRepositry(ApplicationDbContext context, ILogger<ReportRepositry> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<bool> ReportUserAsync(
            ApplicationUser reported,
            ApplicationUser notifier,
            string reason)
        {
            var report = new ReportUserRecord
            {
                Id = Guid.NewGuid(),
                ReportedUserId = reported.Id,
                ReportedUser = reported,
                ReportingUserId = notifier.Id,
                ReportingUser = notifier,
                Description = reason,
                CreatedAt = DateTime.UtcNow,
                Status = ReportStatusEnum.Pending
            };

            await _context.ReportUserRecords.AddAsync(report);
            var result = await _context.SaveChangesAsync();
            return result > 0;
        }

        public async Task<List<UserReportsResponse>> GetUserReportAsync(Guid id)
            => await _context.ReportUserRecords
            .Include(ru => ru.ReportedUser)
            .Include(ru => ru.ReportingUser) 
            .Where(ru =>
                ru.ReportedUserId == id &&
                ru.Status == ReportStatusEnum.Pending
            )
            .OrderBy(ru => ru.CreatedAt)
            .Select(ru => new UserReportsResponse
            {
                ReportedUserName = ru.ReportedUser.UserName,
                ReportedUserEmail = ru.ReportedUser.Email,
                ReportingUserName = ru.ReportingUser.UserName,
                ReportingUserEmail = ru.ReportingUser.Email,
                Reason = ru.Description,
                Status = ru.Status.ToString(),  
                CreatedAt = ru.CreatedAt
            })
            .ToListAsync();

        public async Task<bool> ChangeRepostStatusToAcceptedAsync(
            Guid reportedUserId,
            Guid reportingUserId,
            ApplicationUser admin,
            DateTime createdAt)
        {
            var thisReport = await _context.ReportUserRecords
                .FirstOrDefaultAsync(ru =>
                    ru.ReportedUserId == reportedUserId &&
                    ru.ReportingUserId == reportingUserId &&
                    ru.CreatedAt == createdAt &&
                    ru.Status == ReportStatusEnum.Pending
                );

            if (thisReport == null)
            {
                _logger.LogWarning(
                    "Report not found for ReportedUserId: {ReportedUserId}, ReportingUserId: {ReportingUserId}, CreatedAt: {CreatedAt}",
                    reportedUserId, reportingUserId, createdAt
                );
                return false;
            }

            DateTime now = DateTime.UtcNow;

            thisReport.Status = ReportStatusEnum.Accepted;
            thisReport.ReviewedByAdmin = admin;
            thisReport.ReviewedByAdminId = admin.Id;
            thisReport.ReviewedAt = now;

            var otherPendingReports = await _context.ReportUserRecords
                .Where(ru =>
                    ru.ReportedUserId == reportedUserId &&
                    ru.Status == ReportStatusEnum.Pending &&
                    ru.Id != thisReport.Id 
                )
                .ToListAsync();

            foreach (var report in otherPendingReports)
            {
                report.Status = ReportStatusEnum.Accepted;
                report.ReviewedByAdmin = admin; 
                report.ReviewedByAdminId = admin.Id; 
                report.ReviewedAt = now; 
            }

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Admin {AdminId} accepted report for user {ReportedUserId}. Total {Count} reports accepted.",
                admin.Id, reportedUserId, otherPendingReports.Count + 1
            );

            return result > 0;
        }

        public async Task<bool> ChangeRepostStatusToRejectAsync(
            Guid reportedUserId,
            Guid reportingUserId,
            ApplicationUser admin,
            DateTime createdAt)
        {
            var thisReport = await _context.ReportUserRecords
                .FirstOrDefaultAsync(ru =>
                    ru.ReportedUserId == reportedUserId &&
                    ru.ReportingUserId == reportingUserId &&
                    ru.CreatedAt == createdAt &&
                    ru.Status == ReportStatusEnum.Pending
                );

            if (thisReport == null)
            {
                _logger.LogWarning(
                    "Report not found for ReportedUserId: {ReportedUserId}, ReportingUserId: {ReportingUserId}, CreatedAt: {CreatedAt}",
                    reportedUserId, reportingUserId, createdAt
                );
                return false;
            }

            thisReport.Status = ReportStatusEnum.Rejected;
            thisReport.ReviewedByAdmin = admin;
            thisReport.ReviewedByAdminId = admin.Id;
            thisReport.ReviewedAt = DateTime.UtcNow;

            var result = await _context.SaveChangesAsync();

            _logger.LogInformation(
                "Admin {AdminId} rejected report for user {ReportedUserId} created at {CreatedAt}.",
                admin.Id, reportedUserId, createdAt
            );

            return result > 0;
        }
    }
}
