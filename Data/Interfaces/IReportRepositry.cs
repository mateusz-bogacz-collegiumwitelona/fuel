using Data.Models;
using DTO.Responses;

namespace Data.Interfaces
{
    public interface IReportRepositry
    {
        Task<bool> ReportUserAsync(ApplicationUser reported, ApplicationUser notifier, string reason);
        Task<List<UserReportsResponse>> GetUserReportAsync(Guid id);
        Task<bool> ChangeRepostStatusToAcceptedAsync(Guid reportedUserId, Guid reportingUserId, ApplicationUser admin, DateTime createdAt);
        Task<bool> ChangeRepostStatusToRejectAsync(Guid userId, Guid reportedUserId, ApplicationUser admin, DateTime createdAt);
        Task ClearReports(Guid userId, ApplicationUser admin);
    }
}
