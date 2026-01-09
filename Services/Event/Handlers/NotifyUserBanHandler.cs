using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;
using Services.Interfaces;

namespace Services.Event.Handlers
{
    public class NotifyUserBanHandler : IEventHandler<UserBannedEvent>
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotifyUserBanHandler> _logger;

        public NotifyUserBanHandler(IEmailSender emailSender, ILogger<NotifyUserBanHandler> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task HandleAsync(UserBannedEvent @event)
        {
            var result = await _emailSender.SendLockoutEmailAsync(
                @event.User.Email,
                @event.User.UserName,
                @event.AdminName,
                @event.Days,
                @event.Reason
            );

            if (!result) _logger.LogWarning("Failed to send lockout email to {Email}", @event.User.Email); 
        }
    }
}
