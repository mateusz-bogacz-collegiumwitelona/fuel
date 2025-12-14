using DTO.Requests;

namespace Services.Interfaces
{
    public interface IEmailSender
    {
        Task<bool> SendRegisterConfirmEmailAsync(string email, string userName, string token);
        Task<bool> SendResetPasswordEmailAsync(string email, string userName, string token);
        Task<bool> SendLockoutEmailAsync(string email, string userName, string adminName, int? days, string reason);
        Task<bool> SendUnlockEmailAsync(string email, string userName, string adminName);
        Task<bool> SendAutoUnlockEmailAsync(string email, string userName, string banReason, DateTime bannedAt, DateTime bannedUntil);
        Task<bool> SendPriceProposalStatusEmail(string email, string userName, bool isAccepted, FindStationRequest info, decimal newPrice);
    }
}
