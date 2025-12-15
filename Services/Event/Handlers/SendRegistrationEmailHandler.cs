using Microsoft.Extensions.Logging;
using Services.Event.Interfaces;
using Services.Interfaces;

namespace Services.Event.Handlers
{
    public class SendRegistrationEmailHandler : IEventHandler<UserRegisteredEvent>
    {
        private readonly IEmailSender _emailSender;
        private readonly ILogger<SendRegistrationEmailHandler> _logger;
        public SendRegistrationEmailHandler(IEmailSender emailSender, ILogger<SendRegistrationEmailHandler> _logger)
        {
            _emailSender = emailSender;
            _logger = _logger;
        }

        public async Task HandleAsync(UserRegisteredEvent @event)
        {
            var result = await _emailSender.SendRegisterConfirmEmailAsync(
                @event.User.Email,
                @event.User.UserName,
                @event.ConfirmationToken
                );

            if (result)
            {
                _logger.LogInformation("Sent confirmation email to {Email}", @event.User.Email);
            }
            else
            {
                _logger.LogError("Failed to send confirmation email to {Email}", @event.User.Email);
            }
        }
    }
}
