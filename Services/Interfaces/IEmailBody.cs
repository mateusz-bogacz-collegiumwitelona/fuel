namespace Services.Interfaces
{
    public interface IEmailBody
    {
        string GenerateRegisterConfirmEmailBody(string userName, string confirmationLink, string token);
        string GenerateResetPasswordBody(string userName, string confirmationLink, string token);
        string GenerateLockoutEmailBody(string userName, string adminName, int? days, string reason);
        string GenerateUnlockEmailBody(string userName, string adminName);
        string GenerateAutoUnlockEmailBody(string userName, string banReason, DateTime bannedAt, DateTime bannedUntil);
    }
}