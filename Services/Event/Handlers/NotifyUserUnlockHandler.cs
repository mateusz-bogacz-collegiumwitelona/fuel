using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;
using Services.Interfaces;

namespace Services.Event.Handlers
{
    public class NotifyUserUnlockHandler : IEventHandler<UserUnlockedEvent>
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<NotifyUserUnlockHandler> _logger;

        public NotifyUserUnlockHandler(
            IEmailSender emailSender,
            ILogger<NotifyUserUnlockHandler> logger)
        {
            _emailSender = emailSender;
            _logger = logger;
        }

        public async Task HandleAsync(UserUnlockedEvent @event)
        {
            var result = await _emailSender.SendUnlockEmailAsync(
                @event.User.Email,
                @event.User.UserName,
                @event.AdminName
            );

            if (!result) _logger.LogWarning("Failed to send unlock email to {Email}", @event.User.Email);
        }
    }
}
